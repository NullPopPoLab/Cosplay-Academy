using ExtensibleSaveFormat;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#pragma warning disable 0162 // unreached code

namespace CosplayParty.ME
{
    using ImageDataSource = Dictionary<int, byte[]>;
    using TextureDataSource = List<MaterialTextureProperty>;
    using ShaderDataSource = List<MaterialShader>;
    using RendererDataSource = List<RendererProperty>;
    using ColorDataSource = List<MaterialColorProperty>;
    using FloatDataSource = List<MaterialFloatProperty>;

    public class Common
    {
        public const string PluginName = "MaterialEditor";
        public const string ExtendedDataName = "com.deathweasel.bepinex.materialeditor";
#if KK
        public const string ControllerName = "KK_Plugins.MaterialEditor.MaterialEditorCharaController, KK_MaterialEditor";
#elif KKS
        public const string ControllerName = "KK_Plugins.MaterialEditor.MaterialEditorCharaController, KKS_MaterialEditor";
#endif

        public bool Dump_Load { get { return Settings.Dump_ME_Load; } }
        public bool Dump_Save { get { return Settings.Dump_ME_Save; } }
        public bool Dump_Merge { get { return Settings.Dump_ME_Merge; } }

        public static bool IsSupportedVersion(int ver)
        {
            return ver == 0;
        }

        public const string ImageDataName = "TextureDictionary";
        public const string TextureDataName = "MaterialTexturePropertyList";
        public const string ShaderDataName = "MaterialShaderList";
        public const string RendererDataName = "RendererPropertyList";
        public const string ColorDataName = "MaterialColorPropertyList";
        public const string FloatDataName = "MaterialFloatPropertyList";
    }

    public class PluginSource
    {
        public Dictionary<int, int> TIDConv = new Dictionary<int, int>();
        public ImageDataSource Images;
        public TextureDataSource Texture;
        public ShaderDataSource Shader;
        public RendererDataSource Renderer;
        public ColorDataSource Color;
        public FloatDataSource Float;

        public static PluginSource CreateEmpty()
        {
            var t = new PluginSource();
            t.Images = new ImageDataSource();
            t.Texture = new TextureDataSource();
            t.Shader = new ShaderDataSource();
            t.Renderer = new RendererDataSource();
            t.Color = new ColorDataSource();
            t.Float = new FloatDataSource();
            return t;
        }
    }

    public class TexturePool:Common
    {
        private class Entry
        {
            internal byte[] _image;
            internal int _refcount;

            public Entry(byte[] img)
            {
                _image = img;
            }
        }

        private List<Entry> _pool = new List<Entry>();
        public int Count { get { return _pool.Count; } }

        internal int CountRef(int pid)
        {
            if (pid < 0) return 0;
            if (pid >= _pool.Count) return 0;
            return _pool[pid]._refcount;
        }

        internal void IncRef(int pid)
        {
            if (pid < 0) return;
            if (pid >= _pool.Count) return;
            ++_pool[pid]._refcount;
        }

        internal void DecRef(int pid)
        {
            if (pid < 0) return;
            if (pid >= _pool.Count) return;
            if (_pool[pid]._refcount<1)
            {
                Settings.Logger.LogWarning($"pid {pid} reference underflow");
                return;
            }
            --_pool[pid]._refcount;
        }

        public int Register(byte[] img)
        {
            for(var i = 0; i < _pool.Count; ++i)
            {
                if (_pool[i]._refcount < 1) continue;
                if (!img.SequenceEqual(_pool[i]._image)) continue;
                return i;
            }

            int id = _pool.Count;
            _pool.Add(new Entry(img));
            return id;
        }

        public void Unregister(int pid)
        {
            if (pid < 0) return;
            if (pid >= _pool.Count) return;
            if (_pool[pid]._refcount < 1) return;
            if (--_pool[pid]._refcount > 0) return;
        }

        public byte[] Get(int pid, bool force)
        {
            if (pid < 0) return null;
            if (pid >= _pool.Count) return null;
            if (!force && _pool[pid]._refcount < 1) return null;
            return _pool[pid]._image;
        }

        internal void Pack(PluginSource pack, bool cleanup)
        {
            for (var i = 0; i < _pool.Count; ++i)
            {
                var img = Get(i, !cleanup);
                if (img == null) continue;
                if(Dump_Save) Settings.Logger.LogDebug($"Save Image {i}");
                pack.Images[i] = img;
            }
        }
    }

    public class BaseProps: Common
    {
        public readonly string Caption;
        public List<MaterialTextureProperty> Texture = new List<MaterialTextureProperty>();
        public List<MaterialShader> Shader = new List<MaterialShader>();
        public List<RendererProperty> Renderer = new List<RendererProperty>();
        public List<MaterialColorProperty> Color = new List<MaterialColorProperty>();
        public List<MaterialFloatProperty> Float = new List<MaterialFloatProperty>();

        protected TexturePool _pool;

        public BaseProps(TexturePool pool, string caption)
        {
            Caption = caption;
            _pool = pool;
        }

        //! Register to TexturePool 
        /*! @param tid texid in a card
            @retval poolid in a TexturePool
            @note a TexturePool is shared by a ChaFile and wearable outfits. @n
                and must be managed to avoid conflicting from each outfits. @n
        */
        internal int RegisterTexture(byte[] img)
        {
            return _pool.Register(img);
        }

        internal void AttachTexture(MaterialTextureProperty prop)
        {
            if (prop.TexID != null)
            {
                _pool.IncRef(prop.TexID.Value);
                if(Dump_Load) Settings.Logger.LogDebug($"{Caption} IncRef: type={prop.ObjectType} coord={prop.CoordinateIndex} slot={prop.Slot} tex={prop.TexID} {_pool.CountRef(prop.TexID.Value)-1}=>{_pool.CountRef(prop.TexID.Value)}");
            }
            Texture.Add(prop);
        }

        //! remove texture reference 
        internal void DetachTextures()
        {
            for (var i = 0; i < Texture.Count; ++i)
            {
                var src = Texture[i];
                if (src.TexID == null) continue;

                if (Dump_Load) Settings.Logger.LogDebug($"{Caption} DecRef: type={src.ObjectType} coord={src.CoordinateIndex} slot={src.Slot} tex={src.TexID} {_pool.CountRef(src.TexID.Value)}=>{_pool.CountRef(src.TexID.Value)-1}");
                _pool.DecRef(src.TexID.Value);
            }
        }

        internal void Load(PluginControl.PlugKeeper keeper)
        {
            var src = new PluginSource();
            src.Images = keeper.Read<ImageDataSource>(ImageDataName);
            src.Texture = keeper.Read<TextureDataSource>(TextureDataName);
            src.Shader = keeper.Read<ShaderDataSource>(ShaderDataName);
            src.Renderer = keeper.Read<RendererDataSource>(RendererDataName);
            src.Color = keeper.Read<ColorDataSource>(ColorDataName);
            src.Float = keeper.Read<FloatDataSource>(FloatDataName);

            if (src.Images == null) { }
            else foreach (var t in src.Images)
                {
                    var tid = t.Key;
                    var pid = RegisterTexture(t.Value);
                    src.TIDConv[tid] = pid;
                    if (Dump_Load) Settings.Logger.LogDebug($"TexImage: tid={tid}=>{pid}");
                }

            if (src.Texture == null) { }
            else foreach (var t in src.Texture)
                {
                    // TexID is managed by a chara 
                    // for replace from others, it must renumber to merge them  
                    int? tid = t.TexID;
                    int? pid = null;
                    if (tid != null && src.TIDConv.ContainsKey(tid.Value)) pid = src.TIDConv[tid.Value];

                    if (Dump_Load) Settings.Logger.LogDebug($"Texture: type={t.ObjectType} coord={t.CoordinateIndex} slot={t.Slot} tex={tid}=>{pid} mat={t.MaterialName} prop={t.Property}");
                    var u = new MaterialTextureProperty(t.ObjectType, t.CoordinateIndex, t.Slot, t.MaterialName, t.Property, pid, t.Offset, t.OffsetOriginal, t.Scale, t.ScaleOriginal);
                    AddTextureProp(u);
                }

            if (src.Shader == null) { }
            else foreach (var t in src.Shader)
                {
                    if (Dump_Load) Settings.Logger.LogDebug($"Shader: type={t.ObjectType} coord={t.CoordinateIndex} slot={t.Slot} mat={t.MaterialName} shad={t.ShaderName}");
                    AddShaderProp(t);
                }

            if (src.Renderer == null) { }
            else foreach (var t in src.Renderer)
                {
                    if (Dump_Load) Settings.Logger.LogDebug($"Renderer: type={t.ObjectType} coord={t.CoordinateIndex} slot={t.Slot} name={t.RendererName} val={t.Value}");
                    AddRendererProp(t);
                }

            if (src.Color == null) { }
            else foreach (var t in src.Color)
                {
                    if (Dump_Load) Settings.Logger.LogDebug($"Color: type={t.ObjectType} coord={t.CoordinateIndex} slot={t.Slot} mat={t.MaterialName} {t.Property}={t.Value}");
                    AddColorProp(t);
                }

            if (src.Float == null) { }
            else foreach (var t in src.Float)
                {
                    if (Dump_Load) Settings.Logger.LogDebug($"Float: type={t.ObjectType} coord={t.CoordinateIndex} slot={t.Slot} mat={t.MaterialName} {t.Property}={t.Value}");
                    AddFloatProp(t);
                }
        }

        internal void Save(PluginControl.PlugKeeper keeper, PluginSource pack)
        {
            if (pack.Images.Count > 0)
            {
                if (Dump_Load) Settings.Logger.LogDebug($"Pack: {Caption} has {pack.Images.Count} Images");
                keeper.Write(ImageDataName, pack.Images);
            }
            else
                keeper.Remove(ImageDataName);

            if (pack.Texture.Count > 0)
            {
                if (Dump_Load) Settings.Logger.LogDebug($"Pack: {Caption} has {pack.Texture.Count} Textures");
                keeper.Write(TextureDataName, pack.Texture);
            }
            else
                keeper.Remove(TextureDataName);

            if (pack.Shader.Count > 0)
            {
                if (Dump_Load) Settings.Logger.LogDebug($"Pack: {Caption} has {pack.Shader.Count} Shaders");
                keeper.Write(ShaderDataName, pack.Shader);
            }
            else
                keeper.Remove(ShaderDataName);

            if (pack.Renderer.Count > 0)
            {
                if (Dump_Load) Settings.Logger.LogDebug($"Pack: {Caption} has {pack.Renderer.Count} Renderers");
                keeper.Write(RendererDataName, pack.Renderer);
            }
            else
                keeper.Remove(RendererDataName);

            if (pack.Color.Count > 0)
            {
                if (Dump_Load) Settings.Logger.LogDebug($"Pack: {Caption} has {pack.Color.Count} Colors");
                keeper.Write(ColorDataName, pack.Color);
            }
            else
                keeper.Remove(ColorDataName);

            if (pack.Float.Count > 0)
            {
                if (Dump_Load) Settings.Logger.LogDebug($"Pack: {Caption} has {pack.Float.Count} Floats");
                keeper.Write(FloatDataName, pack.Float);
            }
            else
                keeper.Remove(FloatDataName);

            keeper.Save();
        }

        internal void Merge(BaseProps src, int? coord, int? slot)
        {
            // used src tids 
            var tid2pid = new int?[src._pool.Count];

            for(var i = 0; i < src.Texture.Count; ++i)
            {
                var t = src.Texture[i];
                int? pid = null;
                var tid = t.TexID;
                if (tid != null)
                {
                    if (tid2pid[tid.Value] != null) pid = tid2pid[tid.Value];
                    else
                    {
                        tid2pid[tid.Value] = pid = _pool.Register(src._pool.Get(tid.Value, true));
                        if (Dump_Merge) Settings.Logger.LogDebug($"{Caption} +TexImage: tid={tid}=>{pid}");
                    }
                }
                var u = new MaterialTextureProperty(t.ObjectType, 
                    (coord != null) ? coord.Value : t.CoordinateIndex, 
                    (slot != null) ? slot.Value : t.Slot, 
                    t.MaterialName, t.Property, pid, t.Offset, t.OffsetOriginal, t.Scale, t.ScaleOriginal);
                if (Dump_Merge) Settings.Logger.LogDebug($"{Caption} +Texture: type={t.ObjectType} coord={t.CoordinateIndex}=>{u.CoordinateIndex} slot={t.Slot}=>{u.Slot} tex={t.TexID}=>{u.TexID} mat={t.MaterialName} prop={t.Property}");
                AttachTexture(u);
            }

            for (var i = 0; i < src.Shader.Count; ++i)
            {
                var t = src.Shader[i];
                var u = new MaterialShader(t.ObjectType,
                    (coord != null) ? coord.Value : t.CoordinateIndex,
                    (slot != null) ? slot.Value : t.Slot,
                    t.MaterialName, t.ShaderName, t.ShaderNameOriginal, t.RenderQueue, t.RenderQueueOriginal);
                if (Dump_Merge) Settings.Logger.LogDebug($"{Caption} +Shader: type={t.ObjectType} coord={t.CoordinateIndex}=>{u.CoordinateIndex} slot={t.Slot}=>{u.Slot} mat={t.MaterialName} shad={t.ShaderName}");
                Shader.Add(u);
            }

            for (var i = 0; i < src.Renderer.Count; ++i)
            {
                var t = src.Renderer[i];
                var u = new RendererProperty(t.ObjectType,
                    (coord != null) ? coord.Value : t.CoordinateIndex,
                    (slot != null) ? slot.Value : t.Slot,
                    t.RendererName, t.Property, t.Value, t.ValueOriginal);
                if (Dump_Merge) Settings.Logger.LogDebug($"{Caption} +Renderer: type={t.ObjectType} coord={t.CoordinateIndex}=>{u.CoordinateIndex} slot={t.Slot}=>{u.Slot} name={t.RendererName} val={t.Value}");
                Renderer.Add(u);
            }

            for (var i = 0; i < src.Color.Count; ++i)
            {
                var t = src.Color[i];
                var u = new MaterialColorProperty(t.ObjectType,
                    (coord != null) ? coord.Value : t.CoordinateIndex,
                    (slot != null) ? slot.Value : t.Slot,
                    t.MaterialName, t.Property, t.Value, t.ValueOriginal);
                if (Dump_Merge) Settings.Logger.LogDebug($"{Caption} +Color: type={t.ObjectType} coord={t.CoordinateIndex}=>{u.CoordinateIndex} slot={t.Slot}=>{u.Slot} mat={t.MaterialName} {t.Property}={t.Value}");
                Color.Add(u);
            }

            for (var i = 0; i < src.Float.Count; ++i)
            {
                var t = src.Float[i];
                var u = new MaterialFloatProperty(t.ObjectType,
                    (coord != null) ? coord.Value : t.CoordinateIndex,
                    (slot != null) ? slot.Value : t.Slot,
                    t.MaterialName, t.Property, t.Value, t.ValueOriginal);
                if (Dump_Merge) Settings.Logger.LogDebug($"{Caption} +Float: type={t.ObjectType} coord={t.CoordinateIndex}=>{u.CoordinateIndex} slot={t.Slot}=>{u.Slot} mat={t.MaterialName} {t.Property}={t.Value}");
                Float.Add(u);
            }
        }

        /*! @param saver saving context
            @param coord replacing coord index or not
            @param slot replacing slot or not
        */
        internal void Pack(PluginSource pack, int? coord, int? slot)
        {
            for (var i = 0; i < Texture.Count; ++i)
            {
                var src = Texture[i];
                var dst = new MaterialTextureProperty(src.ObjectType,
                    (coord != null) ? coord.Value : src.CoordinateIndex,
                    (slot != null) ? slot.Value : src.Slot,
                    src.MaterialName, src.Property,
                    src.TexID, src.Offset, src.OffsetOriginal, src.Scale, src.ScaleOriginal);
                if (Dump_Save) Settings.Logger.LogDebug($"Save Texture[{i}]({dst.CoordinateIndex}-{dst.Slot}; {src.MaterialName}:{src.Property})");
                pack.Texture.Add(dst);
            }

            for (var i = 0; i < Shader.Count; ++i)
            {
                var src = Shader[i];
                var dst = new MaterialShader(src.ObjectType, 
                    (coord != null) ? coord.Value : src.CoordinateIndex,
                    (slot != null) ? slot.Value : src.Slot, 
                    src.MaterialName, src.ShaderName, src.ShaderNameOriginal, src.RenderQueue, src.RenderQueueOriginal);
                if (Dump_Save) Settings.Logger.LogDebug($"Save Shader[{i}]({dst.CoordinateIndex}-{dst.Slot}; {src.MaterialName}:{src.ShaderName})");
                pack.Shader.Add(dst);
            }

            for (var i = 0; i < Renderer.Count; ++i)
            {
                var src = Renderer[i];
                var dst = new RendererProperty(src.ObjectType,
                    (coord != null) ? coord.Value : src.CoordinateIndex,
                    (slot != null) ? slot.Value : src.Slot,
                    src.RendererName, src.Property, src.Value, src.ValueOriginal);
                if (Dump_Save) Settings.Logger.LogDebug($"Save Renderer[{i}]({dst.CoordinateIndex}-{dst.Slot}; {src.RendererName}={src.Value})");
                pack.Renderer.Add(dst);
            }

            for (var i = 0; i < Color.Count; ++i)
            {
                var src = Color[i];
                var dst = new MaterialColorProperty(src.ObjectType,
                    (coord != null) ? coord.Value : src.CoordinateIndex,
                    (slot != null) ? slot.Value : src.Slot, 
                    src.MaterialName, src.Property, src.Value, src.ValueOriginal);
                if (Dump_Save) Settings.Logger.LogDebug($"Save Color[{i}]({dst.CoordinateIndex}-{dst.Slot}; {src.MaterialName}:{src.Property}={src.Value})");
                pack.Color.Add(dst);
            }

            for (var i = 0; i < Float.Count; ++i)
            {
                var src = Float[i];
                var dst = new MaterialFloatProperty(src.ObjectType,
                    (coord != null) ? coord.Value : src.CoordinateIndex,
                    (slot != null) ? slot.Value : src.Slot, 
                    src.MaterialName, src.Property, src.Value, src.ValueOriginal);
                if (Dump_Save) Settings.Logger.LogDebug($"Save Float[{i}]({dst.CoordinateIndex}-{dst.Slot}; {src.MaterialName}:{src.Property}={src.Value})");
                pack.Float.Add(dst);
            }
        }

        //! add texture property 
        internal virtual void AddTextureProp(MaterialTextureProperty prop) { }

        //! add shader property 
        internal virtual void AddShaderProp(MaterialShader prop) { }

        //! add renderer property 
        internal virtual void AddRendererProp(RendererProperty prop) { }

        //! add color property 
        internal virtual void AddColorProp(MaterialColorProperty prop) { }

        //! add float property 
        internal virtual void AddFloatProp(MaterialFloatProperty prop) { }
    }

    public class ClothProps : BaseProps
    {
        public ClothProps(TexturePool pool, string caption)
            : base(pool, caption)
        {
        }

        //! add texture property 
        internal override void AddTextureProp(MaterialTextureProperty prop)
        {
            AttachTexture(prop);
        }

        //! add shader property 
        internal override void AddShaderProp(MaterialShader prop)
        {
            Shader.Add(prop);
        }

        //! add renderer property 
        internal override void AddRendererProp(RendererProperty prop)
        {
            Renderer.Add(prop);
        }

        //! add color property 
        internal override void AddColorProp(MaterialColorProperty prop)
        {
            Color.Add(prop);
        }

        //! add float property 
        internal override void AddFloatProp(MaterialFloatProperty prop)
        {
            Float.Add(prop);
        }
    }

    public class AccessoryProps : BaseProps
    {
        public AccessoryProps(TexturePool pool, string caption)
            : base(pool, caption)
        {
        }

        //! add texture property 
        internal override void AddTextureProp(MaterialTextureProperty prop)
        {
            AttachTexture(prop);
        }

        //! add shader property 
        internal override void AddShaderProp(MaterialShader prop)
        {
            Shader.Add(prop);
        }

        //! add renderer property 
        internal override void AddRendererProp(RendererProperty prop)
        {
            Renderer.Add(prop);
        }

        //! add color property 
        internal override void AddColorProp(MaterialColorProperty prop)
        {
            Color.Add(prop);
        }

        //! add float property 
        internal override void AddFloatProp(MaterialFloatProperty prop)
        {
            Float.Add(prop);
        }
    }

    public class CoordProps: BaseProps
    {
        public PluginControl.CoordPlugKeeper Keeper { get; private set; }
        public Dictionary<int, ClothProps> Cloth = new Dictionary<int, ClothProps>();
        public Dictionary<int, AccessoryProps> Accessory = new Dictionary<int, AccessoryProps>();

        //! for chara internal coord 
        public CoordProps(ChaFileControl chaFile, int idx, TexturePool pool)
            : base(pool, chaFile.parameter.fullname + "-" + idx)
        {
            Settings.Logger.LogDebug($"ME.CoordProps({Caption})");

            Keeper = new PluginControl.CoordPlugKeeper(chaFile.coordinate[idx], ExtendedDataName);
        }

        //! from coord card 
        public CoordProps(ChaFileCoordinate coordFile, string caption="")
            : base(new TexturePool(),(caption!="")?caption:(coordFile==null)?"(empty)": coordFile.coordinateName)
        {
            if (coordFile == null)
            {
                Settings.Logger.LogWarning($"{{PluginName}}.CoordProps(empty)");
                return;
            }

            Settings.Logger.LogDebug($"{PluginName}.CoordProps({Caption})");
            Keeper = new PluginControl.CoordPlugKeeper(coordFile, ExtendedDataName);
            if (!Keeper.IsLoaded)
            {
                if (Dump_Load) Settings.Logger.LogDebug($"has no {PluginName} props");
                return;
            }
            if (!IsSupportedVersion(Keeper.Version))
            {
                ClothingLoader.OutdatedMessage($"{PluginName} PluginData", true);
                return;
            }

            Load(Keeper);
        }

#if false
       public void Clear()
        {
            Cloth.Clear();
            Accessory.Clear();
        }
#endif

        //! get by a cloth  
        public ClothProps GetClothProps(int idx, bool force)
        {
            Cloth.TryGetValue(idx, out var ret);
            if (force && ret == null)
            {
                Cloth[idx] = ret = new ClothProps(_pool, Caption + "-" + idx);
            }
            return ret;
        }

        //! get by an accessory 
        public AccessoryProps GetAccessoryProps(int idx, bool force)
        {
            Accessory.TryGetValue(idx, out var ret);
            if (force && ret == null)
            {
                Accessory[idx] = ret = new AccessoryProps(_pool, Caption + "-" + idx);
            }
            return ret;
        }

        //! remove cloth properties 
        public void RemoveClothProps(int idx)
        {
            var prop = GetClothProps(idx, false);
            if (prop == null) return;
            if(Dump_Merge) Settings.Logger.LogDebug($"RemoveClothProps({idx})");
            prop.DetachTextures();
            Cloth.Remove(idx);
        }

        //! remove accessory properties 
        public void RemoveAccessoryProps(int idx)
        {
            var prop = GetAccessoryProps(idx, false);
            if (prop == null) return;
            if (Dump_Merge) Settings.Logger.LogDebug($"RemoveAccessoryProps({idx})");
            prop.DetachTextures();
            Accessory.Remove(idx);
        }

        //! replace cloth properties 
        public void SetClothProps(int? coord, int idx, ClothProps src)
        {
            RemoveClothProps(idx);

            if (src == null) return;
            if (Dump_Merge) Settings.Logger.LogDebug($"SetClothProps({coord},{idx})");
            var prop = GetClothProps(idx, true);
            prop.Merge(src, coord, idx);
        }

        //! replace accessory properties 
        public void SetAccessoryProps(int? coord, int idx, AccessoryProps src)
        {
            RemoveAccessoryProps(idx);

            if (src == null) return;
            if (Dump_Merge) Settings.Logger.LogDebug($"SetAccessoryProps({coord},{idx})");
            var prop = GetAccessoryProps(idx, true);
            prop.Merge(src, coord, idx);
        }

        //! add texture property 
        internal override void AddTextureProp(MaterialTextureProperty prop)
        {
            switch (prop.ObjectType)
            {
                case ObjectType.Clothing:
                    GetClothProps(prop.Slot,true).AddTextureProp(prop);
                    break;

                case ObjectType.Accessory:
                    GetAccessoryProps(prop.Slot,true).AddTextureProp(prop);
                    break;

                default:
                    Settings.Logger.LogWarning($"invalid object type {prop.ObjectType}");
                    break;
            }
        }

        //! add shader property 
        internal override void AddShaderProp(MaterialShader prop)
        {
            switch (prop.ObjectType)
            {
                case ObjectType.Clothing:
                    GetClothProps(prop.Slot,true).AddShaderProp(prop);
                    break;

                case ObjectType.Accessory:
                    GetAccessoryProps(prop.Slot,true).AddShaderProp(prop);
                    break;

                default:
                    Settings.Logger.LogWarning($"invalid object type {prop.ObjectType}");
                    break;
            }
        }

        //! add renderer property 
        internal override void AddRendererProp(RendererProperty prop)
        {
            switch (prop.ObjectType)
            {
                case ObjectType.Clothing:
                    GetClothProps(prop.Slot,true).AddRendererProp(prop);
                    break;

                case ObjectType.Accessory:
                    GetAccessoryProps(prop.Slot,true).AddRendererProp(prop);
                    break;

                default:
                    Settings.Logger.LogWarning($"invalid object type {prop.ObjectType}");
                    break;
            }
        }

        //! add color property 
        internal override void AddColorProp(MaterialColorProperty prop)
        {
            switch (prop.ObjectType)
            {
                case ObjectType.Clothing:
                    GetClothProps(prop.Slot,true).AddColorProp(prop);
                    break;

                case ObjectType.Accessory:
                    GetAccessoryProps(prop.Slot,true).AddColorProp(prop);
                    break;

                default:
                    Settings.Logger.LogWarning($"invalid object type {prop.ObjectType}");
                    break;
            }
        }

        //! add float property 
        internal override void AddFloatProp(MaterialFloatProperty prop)
        {
            switch (prop.ObjectType)
            {
                case ObjectType.Clothing:
                    GetClothProps(prop.Slot,true).AddFloatProp(prop);
                    break;

                case ObjectType.Accessory:
                    GetAccessoryProps(prop.Slot,true).AddFloatProp(prop);
                    break;

                default:
                    Settings.Logger.LogWarning($"invalid object type {prop.ObjectType}");
                    break;
            }
        }

        public PluginSource Pack(bool cleanup)
        {
            var pack = PluginSource.CreateEmpty();
            _pool.Pack(pack, cleanup);
            foreach (var t in Cloth) t.Value.Pack(pack, null, t.Key);
            foreach (var t in Accessory) t.Value.Pack(pack, null, t.Key);
            return pack;
        }

        public void Save(bool cleanup)
        {
            if (Keeper.Target == null)
            {
                Settings.Logger.LogWarning($"no target to save {PluginName} props for {Caption}");
                return;
            }

            Settings.Logger.LogDebug($"Save {PluginName} Props for {Caption}");
            var pack = Pack(cleanup);
            Save(Keeper, pack);
        }

        public void Transfer(ChaFileCoordinate target, string caption, bool cleanup)
        {
            var keeper = new PluginControl.CoordPlugKeeper(target, caption);
            var pack = Pack(cleanup);
            Save(keeper, pack);
        }
    }

    public class CharaProps : BaseProps
    {
        public PluginControl.CharaPlugKeeper Keeper { get; private set; }
        public List<CoordProps> Coord;

        //! from chara card 
        public CharaProps(ChaFileControl chaFile,string caption="")
            : base(new TexturePool(), (caption != null) ? caption : chaFile.parameter.fullname)
        {
            Settings.Logger.LogDebug($"{PluginName}.CharaProps({Caption})");

            Coord = new List<CoordProps>();
            for (var i = 0; i < chaFile.coordinate.Length; ++i)
            {
                // share TextureReferer between chara internal outfits 
                Coord.Add(new CoordProps(chaFile, i, _pool));
            }

            Keeper = new PluginControl.CharaPlugKeeper(chaFile, ExtendedDataName);
            if (!Keeper.IsLoaded)
            {
                if (Dump_Load) Settings.Logger.LogDebug($"has no {PluginName} props");
                return;
            }
            if (!IsSupportedVersion(Keeper.Version))
            {
                ClothingLoader.OutdatedMessage($"{PluginName} PluginData", true);
                return;
            }

            Load(Keeper);
        }

#if false
        public void Clear()
        {
            for (var i = 0; i < Coord.Count; ++i)
            {
                Coord[i].Clear();
            }
        }
#endif

        //! add texture property 
        internal override void AddTextureProp(MaterialTextureProperty prop)
        {
            switch (prop.ObjectType)
            {
                case ObjectType.Character:
                case ObjectType.Hair:
                    AttachTexture(prop);
                    break;

                default:
                    var idx = prop.CoordinateIndex;
                    if (idx < 0 || idx >= Coord.Count)
                    {
                        Settings.Logger.LogWarning($"invalid coord index {idx}");
                        return;
                    }
                    Coord[idx].AddTextureProp(prop);
                    break;
            }
        }

        //! add shader property 
        internal override void AddShaderProp(MaterialShader prop)
        {
            switch (prop.ObjectType)
            {
                case ObjectType.Character:
                case ObjectType.Hair:
                    Shader.Add(prop);
                    break;

                default:
                    var idx = prop.CoordinateIndex;
                    if (idx < 0 || idx >= Coord.Count)
                    {
                        Settings.Logger.LogWarning($"invalid coord index {idx}");
                        return;
                    }
                    Coord[idx].AddShaderProp(prop);
                    break;
            }
        }

        //! add renderer property 
        internal override void AddRendererProp(RendererProperty prop)
        {
            switch (prop.ObjectType)
            {
                case ObjectType.Character:
                case ObjectType.Hair:
                    Renderer.Add(prop);
                    break;

                default:
                    var idx = prop.CoordinateIndex;
                    if (idx < 0 || idx >= Coord.Count)
                    {
                        Settings.Logger.LogWarning($"invalid coord index {idx}");
                        return;
                    }
                    Coord[idx].AddRendererProp(prop);
                    break;
            }
        }

        //! add color property 
        internal override void AddColorProp(MaterialColorProperty prop)
        {
            switch (prop.ObjectType)
            {
                case ObjectType.Character:
                case ObjectType.Hair:
                    Color.Add(prop);
                    break;

                default:
                    var idx = prop.CoordinateIndex;
                    if (idx < 0 || idx >= Coord.Count)
                    {
                        Settings.Logger.LogWarning($"invalid coord index {idx}");
                        return;
                    }
                    Coord[idx].AddColorProp(prop);
                    break;
            }
        }

        //! add float property 
        internal override void AddFloatProp(MaterialFloatProperty prop)
        {
            switch (prop.ObjectType)
            {
                case ObjectType.Character:
                case ObjectType.Hair:
                    Float.Add(prop);
                    break;

                default:
                    var idx = prop.CoordinateIndex;
                    if (idx < 0 || idx >= Coord.Count)
                    {
                        Settings.Logger.LogWarning($"invalid coord index {idx}");
                        return;
                    }
                    Coord[idx].AddFloatProp(prop);
                    break;
            }
        }

        public PluginSource Pack(bool cleanup)
        {
            var pack = PluginSource.CreateEmpty();
            _pool.Pack(pack, cleanup);
            Pack(pack, null, null);
            for (var i = 0; i < Coord.Count; ++i) Coord[i].Pack(pack, i, null);

            return pack;
        }

        public void Save(bool cleanup)
        {
            if (Keeper.Target == null)
            {
                Settings.Logger.LogWarning($"no target to save {PluginName} props for {Caption}");
                return;
            }

            Settings.Logger.LogDebug($"Save {PluginName} Props for {Caption}");
            var pack = Pack(cleanup);
            Save(Keeper, pack);
        }
    }

#region Original Stuff
    public class Support
    {
        public Dictionary<int, TextureContainer> TextureDictionary = new Dictionary<int, TextureContainer>();
        public int SetAndGetTextureID(byte[] textureBytes)
        {
            var highestID = 0;
            foreach (var tex in TextureDictionary)
                if (tex.Value.Data.SequenceEqual(textureBytes))
                    return tex.Key;
                else if (tex.Key > highestID)
                    highestID = tex.Key;

            highestID++;
            TextureDictionary.Add(highestID, new TextureContainer(textureBytes));
            return highestID;
        }
    }

    public enum ObjectType
    {
        /// <summary>
        /// Unknown type, things should never be of this type
        /// </summary>
        Unknown,
        /// <summary>
        /// Clothing
        /// </summary>
        Clothing,
        /// <summary>
        /// Accessory
        /// </summary>
        Accessory,
        /// <summary>
        /// Hair
        /// </summary>
        Hair,
        /// <summary>
        /// Parts of a character
        /// </summary>
        Character
    };

    public sealed class TextureContainer
    {
        public byte[] Data;
        public TextureContainer(byte[] data)
        {
            Data = data;
        }
    }

    [Serializable]
    [MessagePackObject]
    public class RendererProperty
    {
        /// <summary>
        /// Type of the object
        /// </summary>
        [Key("ObjectType")]
        public ObjectType ObjectType;
        /// <summary>
        /// Coordinate index, always 0 except in Koikatsu
        /// </summary>
        [Key("CoordinateIndex")]
        public int CoordinateIndex;
        /// <summary>
        /// Slot of the accessory, hair, or clothing
        /// </summary>
        [Key("Slot")]
        public int Slot;
        /// <summary>
        /// Name of the renderer
        /// </summary>
        [Key("RendererName")]
        public string RendererName;
        /// <summary>
        /// Property type
        /// </summary>
        [Key("Property")]
        public RendererProperties Property;
        /// <summary>
        /// Value
        /// </summary>
        [Key("Value")]
        public string Value;
        /// <summary>
        /// Original value
        /// </summary>
        [Key("ValueOriginal")]
        public string ValueOriginal;

        /// <summary>
        /// Data storage class for renderer properties
        /// </summary>
        /// <param name="objectType">Type of the object</param>
        /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
        /// <param name="slot">Slot of the accessory, hair, or clothing</param>
        /// <param name="rendererName">Name of the renderer</param>
        /// <param name="property">Property type</param>
        /// <param name="value">Value</param>
        /// <param name="valueOriginal">Original</param>
        public RendererProperty(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property, string value, string valueOriginal)
        {
            ObjectType = objectType;
            CoordinateIndex = coordinateIndex;
            Slot = slot;
            RendererName = rendererName.Replace("(Instance)", "").Trim();
            Property = property;
            Value = value;
            ValueOriginal = valueOriginal;
        }
    }

    [Serializable]
    [MessagePackObject]
    public class MaterialFloatProperty
    {
        /// <summary>
        /// Type of the object
        /// </summary>
        [Key("ObjectType")]
        public ObjectType ObjectType;
        /// <summary>
        /// Coordinate index, always 0 except in Koikatsu
        /// </summary>
        [Key("CoordinateIndex")]
        public int CoordinateIndex;
        /// <summary>
        /// Slot of the accessory, hair, or clothing
        /// </summary>
        [Key("Slot")]
        public int Slot;
        /// <summary>
        /// Name of the material
        /// </summary>
        [Key("MaterialName")]
        public string MaterialName;
        /// <summary>
        /// Name of the property
        /// </summary>
        [Key("Property")]
        public string Property;
        /// <summary>
        /// Value
        /// </summary>
        [Key("Value")]
        public string Value;
        /// <summary>
        /// Original value
        /// </summary>
        [Key("ValueOriginal")]
        public string ValueOriginal;

        /// <summary>
        /// Data storage class for float properties
        /// </summary>
        /// <param name="objectType">Type of the object</param>
        /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
        /// <param name="slot">Slot of the accessory, hair, or clothing</param>
        /// <param name="materialName">Name of the material</param>
        /// <param name="property">Name of the property</param>
        /// <param name="value">Value</param>
        /// <param name="valueOriginal">Original value</param>
        public MaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property, string value, string valueOriginal)
        {
            ObjectType = objectType;
            CoordinateIndex = coordinateIndex;
            Slot = slot;
            MaterialName = materialName.Replace("(Instance)", "").Trim();
            Property = property;
            Value = value;
            ValueOriginal = valueOriginal;
        }
    }

    [Serializable]
    [MessagePackObject]
    public class MaterialColorProperty
    {
        /// <summary>
        /// Type of the object
        /// </summary>
        [Key("ObjectType")]
        public ObjectType ObjectType;
        /// <summary>
        /// Coordinate index, always 0 except in Koikatsu
        /// </summary>
        [Key("CoordinateIndex")]
        public int CoordinateIndex;
        /// <summary>
        /// Slot of the accessory, hair, or clothing
        /// </summary>
        [Key("Slot")]
        public int Slot;
        /// <summary>
        /// Name of the material
        /// </summary>
        [Key("MaterialName")]
        public string MaterialName;
        /// <summary>
        /// Name of the property
        /// </summary>
        [Key("Property")]
        public string Property;
        /// <summary>
        /// Value
        /// </summary>
        [Key("Value")]
        public Color Value;
        /// <summary>
        /// Original value
        /// </summary>
        [Key("ValueOriginal")]
        public Color ValueOriginal;

        /// <summary>
        /// Data storage class for color properties
        /// </summary>
        /// <param name="objectType">Type of the object</param>
        /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
        /// <param name="slot">Slot of the accessory, hair, or clothing</param>
        /// <param name="materialName">Name of the material</param>
        /// <param name="property">Name of the property</param>
        /// <param name="value">Value</param>
        /// <param name="valueOriginal">Original value</param>
        public MaterialColorProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property, Color value, Color valueOriginal)
        {
            ObjectType = objectType;
            CoordinateIndex = coordinateIndex;
            Slot = slot;
            MaterialName = materialName.Replace("(Instance)", "").Trim();
            Property = property;
            Value = value;
            ValueOriginal = valueOriginal;
        }
    }

    [Serializable]
    [MessagePackObject]
    public class MaterialTextureProperty
    {
        /// <summary>
        /// Type of the object
        /// </summary>
        [Key("ObjectType")]
        public ObjectType ObjectType;
        /// <summary>
        /// Coordinate index, always 0 except in Koikatsu
        /// </summary>
        [Key("CoordinateIndex")]
        public int CoordinateIndex;
        /// <summary>
        /// Slot of the accessory, hair, or clothing
        /// </summary>
        [Key("Slot")]
        public int Slot;
        /// <summary>
        /// Name of the material
        /// </summary>
        [Key("MaterialName")]
        public string MaterialName;
        /// <summary>
        /// Name of the property
        /// </summary>
        [Key("Property")]
        public string Property;
        /// <summary>
        /// ID of the texture as stored in the texture dictionary
        /// </summary>
        [Key("TexID")]
        public int? TexID;
        /// <summary>
        /// Texture offset value
        /// </summary>
        [Key("Offset")]
        public Vector2? Offset;
        /// <summary>
        /// Texture offset original value
        /// </summary>
        [Key("OffsetOriginal")]
        public Vector2? OffsetOriginal;
        /// <summary>
        /// Texture scale value
        /// </summary>
        [Key("Scale")]
        public Vector2? Scale;
        /// <summary>
        /// Texture scale original value
        /// </summary>
        [Key("ScaleOriginal")]
        public Vector2? ScaleOriginal;

        /// <summary>
        /// Data storage class for texture properties
        /// </summary>
        /// <param name="objectType">Type of the object</param>
        /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
        /// <param name="slot">Slot of the accessory, hair, or clothing</param>
        /// <param name="materialName">Name of the material</param>
        /// <param name="property">Name of the property</param>
        /// <param name="texID">ID of the texture as stored in the texture dictionary</param>
        /// <param name="offset">Texture offset value</param>
        /// <param name="offsetOriginal">Texture offset original value</param>
        /// <param name="scale">Texture scale value</param>
        /// <param name="scaleOriginal">Texture scale original value</param>
        public MaterialTextureProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string property, int? texID = null, Vector2? offset = null, Vector2? offsetOriginal = null, Vector2? scale = null, Vector2? scaleOriginal = null)
        {
            ObjectType = objectType;
            CoordinateIndex = coordinateIndex;
            Slot = slot;
            MaterialName = materialName.Replace("(Instance)", "").Trim();
            Property = property;
            TexID = texID;
            Offset = offset;
            OffsetOriginal = offsetOriginal;
            Scale = scale;
            ScaleOriginal = scaleOriginal;
        }

        /// <summary>
        /// Check if the TexID, Offset, and Scale are all null. Safe to remove this data if true.
        /// </summary>
        /// <returns></returns>
        public bool NullCheck() => TexID == null && Offset == null && Scale == null;
    }

    [Serializable]
    [MessagePackObject]
    public class MaterialShader
    {
        /// <summary>
        /// Type of the object
        /// </summary>
        [Key("ObjectType")]
        public ObjectType ObjectType;
        /// <summary>
        /// Coordinate index, always 0 except in Koikatsu
        /// </summary>
        [Key("CoordinateIndex")]
        public int CoordinateIndex;
        /// <summary>
        /// Slot of the accessory, hair, or clothing
        /// </summary>
        [Key("Slot")]
        public int Slot;
        /// <summary>
        /// Name of the material
        /// </summary>
        [Key("MaterialName")]
        public string MaterialName;
        /// <summary>
        /// Name of the shader
        /// </summary>
        [Key("ShaderName")]
        public string ShaderName;
        /// <summary>
        /// Name of the original shader
        /// </summary>
        [Key("ShaderNameOriginal")]
        public string ShaderNameOriginal;
        /// <summary>
        /// Render queue
        /// </summary>
        [Key("RenderQueue")]
        public int? RenderQueue;
        /// <summary>
        /// Original render queue
        /// </summary>
        [Key("RenderQueueOriginal")]
        public int? RenderQueueOriginal;

        /// <summary>
        /// Data storage class for shader data
        /// </summary>
        /// <param name="objectType">Type of the object</param>
        /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
        /// <param name="slot">Slot of the accessory, hair, or clothing</param>
        /// <param name="materialName">Name of the material</param>
        /// <param name="shaderName">Name of the shader</param>
        /// <param name="shaderNameOriginal">Name of the original shader</param>
        /// <param name="renderQueue">Render queue</param>
        /// <param name="renderQueueOriginal">Original render queue</param>
        public MaterialShader(ObjectType objectType, int coordinateIndex, int slot, string materialName, string shaderName, string shaderNameOriginal, int? renderQueue, int? renderQueueOriginal)
        {
            ObjectType = objectType;
            CoordinateIndex = coordinateIndex;
            Slot = slot;
            MaterialName = materialName.Replace("(Instance)", "").Trim();
            ShaderName = shaderName;
            ShaderNameOriginal = shaderNameOriginal;
            RenderQueue = renderQueue;
            RenderQueueOriginal = renderQueueOriginal;
        }
        /// <summary>
        /// Data storage class for shader data
        /// </summary>
        /// <param name="objectType">Type of the object</param>
        /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
        /// <param name="slot">Slot of the accessory, hair, or clothing</param>
        /// <param name="materialName">Name of the material</param>
        /// <param name="shaderName">Name of the shader</param>
        /// <param name="shaderNameOriginal">Name of the original shader</param>
        public MaterialShader(ObjectType objectType, int coordinateIndex, int slot, string materialName, string shaderName, string shaderNameOriginal)
        {
            ObjectType = objectType;
            CoordinateIndex = coordinateIndex;
            Slot = slot;
            MaterialName = materialName.Replace("(Instance)", "").Trim();
            ShaderName = shaderName;
            ShaderNameOriginal = shaderNameOriginal;
        }
        /// <summary>
        /// Data storage class for shader data
        /// </summary>
        /// <param name="objectType">Type of the object</param>
        /// <param name="coordinateIndex">Coordinate index, always 0 except in Koikatsu</param>
        /// <param name="slot">Slot of the accessory, hair, or clothing</param>
        /// <param name="materialName">Name of the material</param>
        /// <param name="renderQueue">Render queue</param>
        /// <param name="renderQueueOriginal">Original render queue</param>
        public MaterialShader(ObjectType objectType, int coordinateIndex, int slot, string materialName, int? renderQueue, int? renderQueueOriginal)
        {
            ObjectType = objectType;
            CoordinateIndex = coordinateIndex;
            Slot = slot;
            MaterialName = materialName.Replace("(Instance)", "").Trim();
            RenderQueue = renderQueue;
            RenderQueueOriginal = renderQueueOriginal;
        }

        /// <summary>
        /// Check if the shader name and render queue are both null. Safe to delete this data if true.
        /// </summary>
        /// <returns></returns>
        public bool NullCheck() => ShaderName.IsNullOrEmpty() && RenderQueue == null;
    }

    public enum RendererProperties
    {
        /// <summary>
        /// Whether the renderer is enabled
        /// </summary>
        Enabled,
        /// <summary>
        /// ShadowCastingMode of the renderer
        /// </summary>
        ShadowCastingMode,
        /// <summary>
        /// Whether the renderer will receive shadows cast by other objects
        /// </summary>
        ReceiveShadows
    }
#endregion
}