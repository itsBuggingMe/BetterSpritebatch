using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using STVector = System.Numerics.Vector2;
using STMatrix = System.Numerics.Matrix4x4;


namespace BetterSpritebatch;

public class Batcher
{
    internal const int VerticiesPerQuad = 4;
    internal const int IndiciesPerQuad = 6;

    public Batcher(GraphicsDevice graphicsDevice)
    {
        _graphics = graphicsDevice;
    }

    private GraphicsDevice _graphics;
    private VertexPositionColorTextureIndex[] _verticies;
    private ushort[] _indicies;
    private DynamicVertexBuffer _vertexBuffer;
    private DynamicIndexBuffer _indexBuffer;
    private int _nextIndex;


    public BatcherSprite Draw(Texture2D texture, Vector2 position)
    {
        return new BatcherSprite(
            default, 
            texture.Width, 
            texture.Height, 
            _verticies.AsSpan(_nextIndex, VerticiesPerQuad)
            );
    }

    public BatcherSprite Draw(Texture2D texture, Rectangle destination)
    {
        
        
        return new BatcherSprite(
            default,
            texture.Width,
            texture.Height,
            _verticies.AsSpan(_nextIndex, VerticiesPerQuad)
            );
    }

    public void End()
    {

    }

    public ref struct BatcherSprite
    {
        private ref VertexPositionColorTextureIndex _start;

        private STVector _origin;
        private float _textureWidth;
        private float _textureHeight;

        internal BatcherSprite(Vector2 origin, int tWidth, int tHeight, Span<VertexPositionColorTextureIndex> verticies)
        {
            Debug.Assert(verticies.Length == 4);
            _textureWidth = tWidth;
            _textureHeight = tHeight;
            _origin = Unsafe.BitCast<Vector2, STVector>(origin);
            _start = ref MemoryMarshal.GetReference(verticies);
        }

        private ref STVector TL => ref Unsafe.As<Vector2, STVector>(ref _start.Position);
        private ref STVector TR => ref Unsafe.As<Vector2, STVector>(ref Unsafe.Add(ref _start, 1).Position);
        private ref STVector BL => ref Unsafe.As<Vector2, STVector>(ref Unsafe.Add(ref _start, 2).Position);
        private ref STVector BR => ref Unsafe.As<Vector2, STVector>(ref Unsafe.Add(ref _start, 3).Position);

        private ref VertexPositionColorTextureIndex VTL => ref _start;
        private ref VertexPositionColorTextureIndex VTR => ref Unsafe.Add(ref _start, 1);
        private ref VertexPositionColorTextureIndex VBL => ref Unsafe.Add(ref _start, 2);
        private ref VertexPositionColorTextureIndex VBR => ref Unsafe.Add(ref _start, 3);

        public BatcherSprite Rotate(float radians)
        {
            (float sin, float cos) = radians switch
            {
                0 => (0, 1),
                _ => (MathF.Sin(radians), MathF.Cos(radians))
            };

            Rotate(ref TL, sin, cos, _origin);
            Rotate(ref TR, sin, cos, _origin);
            Rotate(ref BL, sin, cos, _origin);
            Rotate(ref BR, sin, cos, _origin);

            return this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void Rotate(ref STVector vec, float sin, float cos, STVector origin)
            {
                vec -= origin;
                float x = vec.X;
                vec.X = x * cos - vec.Y * sin;
                vec.Y = x * sin + vec.Y * cos;
                vec += origin;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BatcherSprite SetSource(Rectangle sourceRectangle)
        {
            Vector64<float> lower = Vector64.Create((float)sourceRectangle.X, sourceRectangle.Y);
            Vector64<float> higher = Vector64.Create((float)sourceRectangle.Height, sourceRectangle.Width);
            Vector128<float> corners = Vector128.Create(lower, lower + higher);
            corners /= Vector128.Create(_textureWidth, _textureHeight, _textureWidth, _textureHeight);
            //lx,ty,rx,by
            VTL.TextureCoordinate = Unsafe.BitCast<Vector64<float>, Vector2>(corners.GetLower());
            VBR.TextureCoordinate = Unsafe.BitCast<Vector64<float>, Vector2>(corners.GetUpper());

            corners = Vector128.Shuffle(corners, Vector128.Create(2, 1, 0, 3));

            //rx,ty,lx,by
            VTR.TextureCoordinate = Unsafe.BitCast<Vector64<float>, Vector2>(corners.GetLower());
            VBL.TextureCoordinate = Unsafe.BitCast<Vector64<float>, Vector2>(corners.GetUpper());

            return this;
        }
        
        public BatcherSprite Transform(Matrix matrix)
        {
            TL = STVector.Transform(TL, Unsafe.BitCast<Matrix, STMatrix>(matrix));
            TR = STVector.Transform(TR, Unsafe.BitCast<Matrix, STMatrix>(matrix));
            BL = STVector.Transform(BL, Unsafe.BitCast<Matrix, STMatrix>(matrix));
            BR = STVector.Transform(TR, Unsafe.BitCast<Matrix, STMatrix>(matrix));
            return this;
        }

        public BatcherSprite FlipVertically()
        {
            ref var a = ref VTL.TextureCoordinate.Y;
            ref var b = ref VBR.TextureCoordinate.Y;
            (a, b)  = (b, a);
            return this;
        }

        public BatcherSprite FlipHorzontally()
        {
            ref var a = ref VTL.TextureCoordinate.X;
            ref var b = ref VBR.TextureCoordinate.X;
            (a, b)  = (b, a);
            return this;
        }

        public BatcherSprite ApplyEffect(SpriteEffects effects)
        {
            if((effects & SpriteEffects.FlipHorizontally) != 0)
                FlipHorzontally();
            if((effects & SpriteEffects.FlipVertically) != 0)
                FlipVertically();
            return this;
        }

        public BatcherSprite Scale(Vector2 multipler)
        {
            if(Vector256.IsHardwareAccelerated)
            {
                //todo: check if jit opts this
                Vector256<float> pos = Vector256.Create(TL.X, TL.Y, TR.X, TR.Y, BL.X, BL.Y, BR.X, BR.Y);
                Vector256<float> origin = Vector256.Create(Unsafe.BitCast<STVector, long>(_origin)).AsSingle();
                
                pos -= origin;
                pos *= Vector256.Create(Unsafe.BitCast<Vector2, long>(multipler)).AsSingle();
                pos += origin;

                TL = Unsafe.BitCast<Vector64<float>, STVector>(pos.GetLower().GetLower());
                TR = Unsafe.BitCast<Vector64<float>, STVector>(pos.GetLower().GetUpper());
                BL = Unsafe.BitCast<Vector64<float>, STVector>(pos.GetUpper().GetLower());
                TR = Unsafe.BitCast<Vector64<float>, STVector>(pos.GetUpper().GetUpper());
            }
            else
            {
                Vector128<float> lower = Vector128.Create(TL.X, TL.Y, TR.X, TR.Y);
                Vector128<float> upper = Vector128.Create(BL.X, BL.Y, BR.X, BR.Y);
                Vector128<float> origin = Vector128.Create(Unsafe.BitCast<STVector, long>(_origin)).AsSingle();
                
                lower -= origin;
                upper -= origin;

                Vector128<float> mul = Vector128.Create(Unsafe.BitCast<Vector2, long>(multipler)).AsSingle();
                lower *= mul;
                upper *= mul;

                lower += origin;
                upper -= origin;

                TL = Unsafe.BitCast<Vector64<float>, STVector>(lower.GetLower());
                TR = Unsafe.BitCast<Vector64<float>, STVector>(lower.GetUpper());
                BL = Unsafe.BitCast<Vector64<float>, STVector>(upper.GetLower());
                TR = Unsafe.BitCast<Vector64<float>, STVector>(upper.GetUpper());
            }

            return this;
        }

        public BatcherSprite Scale(float multipler)
        {
            if(Vector256.IsHardwareAccelerated)
            {
                //todo: check if jit opts this
                Vector256<float> pos = Vector256.Create(TL.X, TL.Y, TR.X, TR.Y, BL.X, BL.Y, BR.X, BR.Y);
                Vector256<float> origin = Vector256.Create(Unsafe.BitCast<STVector, long>(_origin)).AsSingle();
                
                pos -= origin;
                pos *= Vector256.Create(multipler);
                pos += origin;

                TL = Unsafe.BitCast<Vector64<float>, STVector>(pos.GetLower().GetLower());
                TR = Unsafe.BitCast<Vector64<float>, STVector>(pos.GetLower().GetUpper());
                BL = Unsafe.BitCast<Vector64<float>, STVector>(pos.GetUpper().GetLower());
                TR = Unsafe.BitCast<Vector64<float>, STVector>(pos.GetUpper().GetUpper());
            }
            else
            {
                Vector128<float> lower = Vector128.Create(TL.X, TL.Y, TR.X, TR.Y);
                Vector128<float> upper = Vector128.Create(BL.X, BL.Y, BR.X, BR.Y);
                Vector128<float> origin = Vector128.Create(Unsafe.BitCast<STVector, long>(_origin)).AsSingle();
                lower += origin;
                upper += origin;
                
                Vector128<float> mul = Vector128.Create(multipler);
                lower *= mul;
                upper *= mul;

                lower -= origin;
                upper -= origin;
                
                TL = Unsafe.BitCast<Vector64<float>, STVector>(lower.GetLower());
                TR = Unsafe.BitCast<Vector64<float>, STVector>(lower.GetUpper());
                BL = Unsafe.BitCast<Vector64<float>, STVector>(upper.GetLower());
                TR = Unsafe.BitCast<Vector64<float>, STVector>(upper.GetUpper());
            }

            return this;        
        }

        public BatcherSprite Tint(Color color)
        {
            VTL.Color = color;
            VTR.Color = color;
            VBL.Color = color;
            VBR.Color = color;
            return this;
        }
    }
}