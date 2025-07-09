using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using STVector = System.Numerics.Vector2;
using STMatrix = System.Numerics.Matrix4x4;
using Microsoft.Xna.Framework.Content;


namespace BetterSpritebatch;

public class Batcher
{
    internal struct HandleLookup(Rectangle bounds, Vector2 tL, Vector2 tR, Vector2 bL, Vector2 bR)
    {
        public Rectangle Bounds = bounds;
        public Vector2 TL = tL;
        public Vector2 TR = tR;
        public Vector2 BL = bL;
        public Vector2 BR = bR;
        // All textures supported have width, so TR.X must be non zero for it to not be default.
        public bool IsDefault => TR.X == default;
    }

    internal const int DefaultInitialSpriteCapacity = 42;
    internal const int DefaultAtlasSize = 512;
    internal const int VerticiesPerQuad = 4;
    internal const int IndiciesPerQuad = 6;
    internal const int MaxQuadsPerBatch = 65532;

    private static int _nextId = 0;
    private static ReadOnlySpan<uint> IndexPattern => [0, 1, 2, 1, 2, 4];

    internal readonly int Id = Interlocked.Increment(ref _nextId);

    private Texture2D _atlas;
    private SkylinePacker _packer;
    private HandleLookup[] _handleLookup = [];
    private Stack<(Texture2D Texture, Rectangle AtlasBounds, bool NeedsDispose)> _texturesToBuild = [];


    private GraphicsDevice _graphics;
    private DynamicVertexBuffer _vertexBuffer;
    private DynamicIndexBuffer _indexBuffer;
    private VertexPositionColorTexture2D[] _verticies;
    private uint[] _indicies;
    private int _nextIndex;

    public Batcher(
        GraphicsDevice graphicsDevice,
        ContentManager contentManager,
        int initalSpriteCapacity = DefaultInitialSpriteCapacity,
        int initalAtlasSize = DefaultAtlasSize)
    {
        _graphics = graphicsDevice;

        _verticies = new VertexPositionColorTexture2D[initalSpriteCapacity * VerticiesPerQuad];
        _indicies = new uint[initalSpriteCapacity * IndiciesPerQuad];
        _handleLookup = new HandleLookup[8];

        _packer = new SkylinePacker(initalAtlasSize, initalAtlasSize, Resize);
        _atlas = new Texture2D(graphicsDevice, initalAtlasSize, initalAtlasSize);

        _vertexBuffer = new DynamicVertexBuffer(graphicsDevice, default(VertexPositionColorTexture2D).VertexDeclaration, initalSpriteCapacity * VerticiesPerQuad, BufferUsage.WriteOnly);
        _indexBuffer = new DynamicIndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, initalSpriteCapacity * IndiciesPerQuad, BufferUsage.WriteOnly);
    }

    public TextureHandle CreateHandle(Texture2D texture)
    {
        var result = texture.GetTextureHandle(this);

        ref HandleLookup coordToSet = ref TextureHelper.GetValueOrResize(ref _handleLookup, result.Value);

        if (coordToSet.IsDefault)
            InitalizeTextureCoordsFor(texture, ref coordToSet);

        return result;
    }

    public BatcherSprite Draw(Texture2D texture, Vector2 position) => Draw(CreateHandle(texture), position);

    public BatcherSprite Draw(TextureHandle handle, Vector2 position)
    {
        if (handle.BatcherId != Id)
            ThrowInvalidTextureHandle();

        HandleLookup[] coords = _handleLookup;
        int index = handle.Value;

        if (!((uint)index < (uint)coords.Length))
            ThrowUnreachableException();

        ref HandleLookup slot = ref coords[index];

        var verts = EnsureCapacity();

        return new BatcherSprite(position, slot.Bounds.Width, slot.Bounds.Height, verts)
            .SetTextcoords(slot);
    }

    private Span<VertexPositionColorTexture2D> EnsureCapacity()
    {
        if(_nextIndex + 4 > )
        {

        }
        
        return [];
    }

    public void Submit(BlendState? blendState = null, SamplerState? samplerState = null, DepthStencilState? depthStencilState = null, RasterizerState? rasterizerState = null)
    {
        _graphics.BlendState = blendState ?? BlendState.AlphaBlend;
        _graphics.VertexSamplerStates[0] = samplerState ?? SamplerState.LinearClamp;
        _graphics.DepthStencilState = depthStencilState ?? DepthStencilState.Default;
        _graphics.RasterizerState = rasterizerState ?? RasterizerState.CullCounterClockwise;


        _graphics.SetVertexBuffer();

        int chunkEnd = Math.Min(MaxQuadsPerBatch, _nextIndex);
        for (int chunkStart = 0; chunkEnd < _nextIndex; chunkStart += MaxQuadsPerBatch, chunkEnd = Math.Min(MaxQuadsPerBatch, _nextIndex))
        {
            _graphics.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, chunkStart, 0, 10);
        }
    }
    
    private void InitalizeTextureCoordsFor(Texture2D texture2D, ref HandleLookup coords)
    {
        Point position = _packer.Pack(texture2D.Width, texture2D.Height);

        Rectangle atlasBounds = new(position, texture2D.Bounds.Size);

        _texturesToBuild.Push((texture2D, atlasBounds, false));

        coords.Bounds = atlasBounds;

        SetTextCoords(ref coords, Vector2.One / _atlas.Bounds.Size.ToVector2());
    }

    private Point Resize(Point value)
    {
        Point newSize = new(value.X * 2, value.Y * 2);

        _texturesToBuild.Push((_atlas, new Rectangle(0, 0, value.X, value.Y), true));

        _atlas = new Texture2D(_graphics, newSize.X, newSize.Y);

        var recep = Vector2.One / newSize.ToVector2();
        foreach (ref var coord in _handleLookup.AsSpan())
        {
            SetTextCoords(ref coord, recep);
        }

        return newSize;
    }

    private static void SetTextCoords(ref HandleLookup coords, Vector2 atlasSizeReciprical)
    {
        Vector2 position = coords.Bounds.Location.ToVector2();
        Vector2 brCorner = position + coords.Bounds.Size.ToVector2();

        position *= atlasSizeReciprical;
        brCorner *= atlasSizeReciprical;

        coords.TL = position;
        coords.BR = brCorner;

        coords.TR = new Vector2(brCorner.X, position.Y);
        coords.BL = new Vector2(position.X, brCorner.Y);
    }

    public ref struct BatcherSprite
    {
        private ref VertexPositionColorTexture2D _start;

        private STVector _origin;
        private float _textureWidth;
        private float _textureHeight;

        internal BatcherSprite(Vector2 origin, int tWidth, int tHeight, Span<VertexPositionColorTexture2D> verticies)
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

        private ref VertexPositionColorTexture2D VTL => ref _start;
        private ref VertexPositionColorTexture2D VTR => ref Unsafe.Add(ref _start, 1);
        private ref VertexPositionColorTexture2D VBL => ref Unsafe.Add(ref _start, 2);
        private ref VertexPositionColorTexture2D VBR => ref Unsafe.Add(ref _start, 3);

        internal BatcherSprite SetTextcoords(HandleLookup textCoord)
        {
            VTL.TextureCoordinate = textCoord.TL;
            VTR.TextureCoordinate = textCoord.TR;
            VBL.TextureCoordinate = textCoord.BL;
            VBR.TextureCoordinate = textCoord.BR;
            return this;
        }

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
            (a, b) = (b, a);
            return this;
        }

        public BatcherSprite FlipHorizontally()
        {
            ref var a = ref VTL.TextureCoordinate.X;
            ref var b = ref VBR.TextureCoordinate.X;
            (a, b) = (b, a);
            return this;
        }

        public BatcherSprite ApplyEffect(SpriteEffects effects)
        {
            if ((effects & SpriteEffects.FlipHorizontally) != 0)
                FlipHorizontally();
            if ((effects & SpriteEffects.FlipVertically) != 0)
                FlipVertically();
            return this;
        }

        public BatcherSprite Scale(Vector2 multipler)
        {
            if (Vector256.IsHardwareAccelerated)
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
            if (Vector256.IsHardwareAccelerated)
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

    private static void ThrowInvalidTextureHandle()
    {
        throw new ArgumentException("Texture handle invalid.");
    }

    private static void ThrowUnreachableException()
    {
        throw new UnreachableException();
    }
}