using ExtensibleSaveFormat;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#pragma warning disable 0162 // unreached code

namespace CosplayParty.ME
{
    public static class ME_Common
    {
        public const bool Dump_Load = true;
        public const bool Dump_Save = true;
        public const bool Dump_Merge = true;

#if KK
        public const string ControllerName = "KK_Plugins.MaterialEditor.MaterialEditorCharaController, KK_MaterialEditor";
#elif KKS
        public const string ControllerName = "KK_Plugins.MaterialEditor.MaterialEditorCharaController, KKS_MaterialEditor";
#endif
        public const string ExtendedDataName = "com.deathweasel.bepinex.materialeditor";

        internal static void Classify(ME_Loader loader, PluginData pluginData)
        {
            if (pluginData == null)
            {
                if (Dump_Load) Settings.Logger.LogDebug($"has no Material props");
                return;
            }
            if (pluginData.version != 0)
            {
                ClothingLoader.OutdatedMessage("Material Editor PluginData", true);
                return;
            }

            if (pluginData.data.TryGetValue("TextureDictionary", out var texDic) && texDic != null)
            {
                foreach (var t in MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>((byte[])texDic))
                {
                    var tid = t.Key;
                    var pid = loader.Target.RegisterTexture(t.Value);
                    loader.TIDConv[tid] = pid;
                    if (Dump_Load) Settings.Logger.LogDebug($"TexImage: tid={tid}=>{pid}");
                }
            }

            if (pluginData.data.TryGetValue("MaterialTexturePropertyList", out var materialTextureProperties) && materialTextureProperties != null)
            {
                foreach (var t in MessagePackSerializer.Deserialize<List<MaterialTextureProperty>>((byte[])materialTextureProperties))
                {
                    // TexID is managed by a chara 
                    // for replace from others, it must renumber to merge them  
                    int? tid = t.TexID;
                    int? pid = null;
                    if (tid != null && loader.TIDConv.ContainsKey(tid.Value)) pid = loader.TIDConv[tid.Value];

                    if (Dump_Load) Settings.Logger.LogDebug($"Texture: type={t.ObjectType} coord={t.CoordinateIndex} slot={t.Slot} tex={tid}=>{pid} mat={t.MaterialName} prop={t.Property}");
                    var u = new MaterialTextureProperty(t.ObjectType, t.CoordinateIndex, t.Slot, t.MaterialName, t.Property, pid, t.Offset, t.OffsetOriginal, t.Scale, t.ScaleOriginal);
                    loader.Target.AddTextureProp(u);
                }
            }

            if (pluginData.data.TryGetValue("MaterialShaderList", out var shaderProperties) && shaderProperties != null)
            {
                foreach (var t in MessagePackSerializer.Deserialize<List<MaterialShader>>((byte[])shaderProperties))
                {
                    if (Dump_Load) Settings.Logger.LogDebug($"Shader: type={t.ObjectType} coord={t.CoordinateIndex} slot={t.Slot} mat={t.MaterialName} shad={t.ShaderName}");
                    loader.Target.AddShaderProp(t);
                }
            }

            if (pluginData.data.TryGetValue("RendererPropertyList", out var rendererProperties) && rendererProperties != null)
            {
                foreach (var t in MessagePackSerializer.Deserialize<List<RendererProperty>>((byte[])rendererProperties))
                {
                    if (Dump_Load) Settings.Logger.LogDebug($"Renderer: type={t.ObjectType} coord={t.CoordinateIndex} slot={t.Slot} name={t.RendererName} val={t.Value}");
                    loader.Target.AddRendererProp(t);
                }
            }

            if (pluginData.data.TryGetValue("MaterialColorPropertyList", out var materialColorProperties) && materialColorProperties != null)
            {
                foreach (var t in MessagePackSerializer.Deserialize<List<MaterialColorProperty>>((byte[])materialColorProperties))
                {
                    if (Dump_Load) Settings.Logger.LogDebug($"Color: type={t.ObjectType} coord={t.CoordinateIndex} slot={t.Slot} mat={t.MaterialName} {t.Property}={t.Value}");
                    loader.Target.AddColorProp(t);
                }
            }

            if (pluginData.data.TryGetValue("MaterialFloatPropertyList", out var materialFloatProperties) && materialFloatProperties != null)
            {
                foreach (var t in MessagePackSerializer.Deserialize<List<MaterialFloatProperty>>((byte[])materialFloatProperties))
                {
                    if (Dump_Load) Settings.Logger.LogDebug($"Float: type={t.ObjectType} coord={t.CoordinateIndex} slot={t.Slot} mat={t.MaterialName} {t.Property}={t.Value}");
                    loader.Target.AddFloatProp(t);
                }
            }
        }
    }

    public interface IClassifiable
    {
        //! Register to TexturePool 
        /*! @param tid texid in a card
            @retval poolid in a TexturePool
            @note a TexturePool is shared by a ChaFile and wearable outfits. @n
                and must be managed to avoid conflicting from each outfits. @n
        */
        int RegisterTexture(byte[] img);

        //! add texture property 
        void AddTextureProp(MaterialTextureProperty prop);

        //! add shader property 
        void AddShaderProp(MaterialShader prop);

        //! add renderer property 
        void AddRendererProp(RendererProperty prop);

        //! add color property 
        void AddColorProp(MaterialColorProperty prop);

        //! add float property 
        void AddFloatProp(MaterialFloatProperty prop);
    }

    internal class ME_Loader
    {
        public IClassifiable Target { get; private set; }
        public Dictionary<int, int> TIDConv = new Dictionary<int, int>();

        public ME_Loader(IClassifiable target)
        {
            Target = target;
        }
    }

    internal class ME_Saver
    {
        public string Caption { get; private set; }
        public Dictionary<int, byte[]> Images = new Dictionary<int, byte[]>();
        public List<MaterialTextureProperty> Texture = new List<MaterialTextureProperty>();
        public List<MaterialShader> Shader = new List<MaterialShader>();
        public List<RendererProperty> Renderer = new List<RendererProperty>();
        public List<MaterialFloatProperty> Float = new List<MaterialFloatProperty>();
        public List<MaterialColorProperty> Color = new List<MaterialColorProperty>();

        public ME_Saver(string caption)
        {
            Caption = caption;
        }

        public PluginData Pack()
        {
            var pack = new PluginData();

            if (Images.Count > 0)
            {
                if (ME_Common.Dump_Load) Settings.Logger.LogDebug($"Pack: {Caption} has {Images.Count} Images");
                pack.data.Add("TextureDictionary", MessagePackSerializer.Serialize(Images));
            }
            else
                pack.data.Add("TextureDictionary", null);

            if (Texture.Count > 0)
            {
                if (ME_Common.Dump_Load) Settings.Logger.LogDebug($"Pack: {Caption} has {Texture.Count} Textures");
                pack.data.Add("MaterialTexturePropertyList", MessagePackSerializer.Serialize(Texture));
            }
            else
                pack.data.Add("MaterialTexturePropertyList", null);

            if (Shader.Count > 0)
            {
                if (ME_Common.Dump_Load) Settings.Logger.LogDebug($"Pack: {Caption} has {Shader.Count} Shaders");
                pack.data.Add("MaterialShaderList", MessagePackSerializer.Serialize(Shader));
            }
            else
                pack.data.Add("MaterialShaderList", null);

            if (Renderer.Count > 0)
            {
                if (ME_Common.Dump_Load) Settings.Logger.LogDebug($"Pack: {Caption} has {Renderer.Count} Renderers");
                pack.data.Add("RendererPropertyList", MessagePackSerializer.Serialize(Renderer));
            }
            else
                pack.data.Add("RendererPropertyList", null);

            if (Color.Count > 0)
            {
                if (ME_Common.Dump_Load) Settings.Logger.LogDebug($"Pack: {Caption} has {Color.Count} Colors");
                pack.data.Add("MaterialColorPropertyList", MessagePackSerializer.Serialize(Color));
            }
            else
                pack.data.Add("MaterialColorPropertyList", null);

            if (Float.Count > 0)
            {
                if (ME_Common.Dump_Load) Settings.Logger.LogDebug($"Pack: {Caption} has {Float.Count} Floats");
                pack.data.Add("MaterialFloatPropertyList", MessagePackSerializer.Serialize(Float));
            }
            else
                pack.data.Add("MaterialFloatPropertyList", null);

            return pack;
        }
    }

    public class ME_TexturePool
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

        internal void Save(ME_Saver saver, bool cleanup)
        {
            for (var i = 0; i < _pool.Count; ++i)
            {
                var img = Get(i, !cleanup);
                if (img == null) continue;
                if(ME_Common.Dump_Save) Settings.Logger.LogDebug($"Save Image {i} for {saver.Caption}");
                saver.Images[i] = img;
            }
        }
    }

    public class ME_MaterialProps
    {
        public readonly string Caption;
        public List<MaterialTextureProperty> Texture = new List<MaterialTextureProperty>();
        public List<MaterialShader> Shader = new List<MaterialShader>();
        public List<RendererProperty> Renderer = new List<RendererProperty>();
        public List<MaterialColorProperty> Color = new List<MaterialColorProperty>();
        public List<MaterialFloatProperty> Float = new List<MaterialFloatProperty>();

        protected ME_TexturePool _pool;

        public ME_MaterialProps(ME_TexturePool pool, string caption)
        {
            Caption = caption;
            _pool = pool;
        }

        //! Register to TexturePool 
        public int RegisterTexture(byte[] img)
        {
            return _pool.Register(img);
        }

        internal void AttachTexture(MaterialTextureProperty prop)
        {
            if (prop.TexID != null)
            {
                _pool.IncRef(prop.TexID.Value);
                if(ME_Common.Dump_Load)Settings.Logger.LogDebug($"{Caption} IncRef: type={prop.ObjectType} coord={prop.CoordinateIndex} slot={prop.Slot} tex={prop.TexID} {_pool.CountRef(prop.TexID.Value)-1}=>{_pool.CountRef(prop.TexID.Value)}");
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

                if (ME_Common.Dump_Load) Settings.Logger.LogDebug($"{Caption} DecRef: type={src.ObjectType} coord={src.CoordinateIndex} slot={src.Slot} tex={src.TexID} {_pool.CountRef(src.TexID.Value)}=>{_pool.CountRef(src.TexID.Value)-1}");
                _pool.DecRef(src.TexID.Value);
            }
        }

        internal void Merge(ME_MaterialProps src, int? coord, int? slot)
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
                        if (ME_Common.Dump_Merge) Settings.Logger.LogDebug($"{Caption} +TexImage: tid={tid}=>{pid}");
                    }
                }
                var u = new MaterialTextureProperty(t.ObjectType, 
                    (coord != null) ? coord.Value : t.CoordinateIndex, 
                    (slot != null) ? slot.Value : t.Slot, 
                    t.MaterialName, t.Property, pid, t.Offset, t.OffsetOriginal, t.Scale, t.ScaleOriginal);
                if (ME_Common.Dump_Merge) Settings.Logger.LogDebug($"{Caption} +Texture: type={t.ObjectType} coord={t.CoordinateIndex}=>{u.CoordinateIndex} slot={t.Slot}=>{u.Slot} tex={t.TexID}=>{u.TexID} mat={t.MaterialName} prop={t.Property}");
                AttachTexture(u);
            }

            for (var i = 0; i < src.Shader.Count; ++i)
            {
                var t = src.Shader[i];
                var u = new MaterialShader(t.ObjectType,
                    (coord != null) ? coord.Value : t.CoordinateIndex,
                    (slot != null) ? slot.Value : t.Slot,
                    t.MaterialName, t.ShaderName, t.ShaderNameOriginal, t.RenderQueue, t.RenderQueueOriginal);
                if (ME_Common.Dump_Merge) Settings.Logger.LogDebug($"{Caption} +Shader: type={t.ObjectType} coord={t.CoordinateIndex}=>{u.CoordinateIndex} slot={t.Slot}=>{u.Slot} mat={t.MaterialName} shad={t.ShaderName}");
                Shader.Add(u);
            }

            for (var i = 0; i < src.Renderer.Count; ++i)
            {
                var t = src.Renderer[i];
                var u = new RendererProperty(t.ObjectType,
                    (coord != null) ? coord.Value : t.CoordinateIndex,
                    (slot != null) ? slot.Value : t.Slot,
                    t.RendererName, t.Property, t.Value, t.ValueOriginal);
                if (ME_Common.Dump_Merge) Settings.Logger.LogDebug($"{Caption} +Renderer: type={t.ObjectType} coord={t.CoordinateIndex}=>{u.CoordinateIndex} slot={t.Slot}=>{u.Slot} name={t.RendererName} val={t.Value}");
                Renderer.Add(u);
            }

            for (var i = 0; i < src.Color.Count; ++i)
            {
                var t = src.Color[i];
                var u = new MaterialColorProperty(t.ObjectType,
                    (coord != null) ? coord.Value : t.CoordinateIndex,
                    (slot != null) ? slot.Value : t.Slot,
                    t.MaterialName, t.Property, t.Value, t.ValueOriginal);
                if (ME_Common.Dump_Merge) Settings.Logger.LogDebug($"{Caption} +Color: type={t.ObjectType} coord={t.CoordinateIndex}=>{u.CoordinateIndex} slot={t.Slot}=>{u.Slot} mat={t.MaterialName} {t.Property}={t.Value}");
                Color.Add(u);
            }

            for (var i = 0; i < src.Float.Count; ++i)
            {
                var t = src.Float[i];
                var u = new MaterialFloatProperty(t.ObjectType,
                    (coord != null) ? coord.Value : t.CoordinateIndex,
                    (slot != null) ? slot.Value : t.Slot,
                    t.MaterialName, t.Property, t.Value, t.ValueOriginal);
                if (ME_Common.Dump_Merge) Settings.Logger.LogDebug($"{Caption} +Float: type={t.ObjectType} coord={t.CoordinateIndex}=>{u.CoordinateIndex} slot={t.Slot}=>{u.Slot} mat={t.MaterialName} {t.Property}={t.Value}");
                Float.Add(u);
            }
        }

        /*! @param saver saving context
            @param coord replacing coord index or not
            @param slot replacing slot or not
        */
        internal void Save(ME_Saver saver, int? coord, int? slot)
        {
            for (var i = 0; i < Texture.Count; ++i)
            {
                var src = Texture[i];
                var dst = new MaterialTextureProperty(src.ObjectType,
                    (coord != null) ? coord.Value : src.CoordinateIndex,
                    (slot != null) ? slot.Value : src.Slot,
                    src.MaterialName, src.Property,
                    src.TexID, src.Offset, src.OffsetOriginal, src.Scale, src.ScaleOriginal);
                if (ME_Common.Dump_Save) Settings.Logger.LogDebug($"Save Texture[{i}]({dst.CoordinateIndex}-{dst.Slot}) for {saver.Caption}");
                saver.Texture.Add(dst);
            }

            for (var i = 0; i < Shader.Count; ++i)
            {
                var src = Shader[i];
                var dst = new MaterialShader(src.ObjectType, 
                    (coord != null) ? coord.Value : src.CoordinateIndex,
                    (slot != null) ? slot.Value : src.Slot, 
                    src.MaterialName, src.ShaderName, src.ShaderNameOriginal, src.RenderQueue, src.RenderQueueOriginal);
                if (ME_Common.Dump_Save) Settings.Logger.LogDebug($"Save Shader[{i}]({dst.CoordinateIndex}-{dst.Slot}) for {saver.Caption}");
                saver.Shader.Add(dst);
            }

            for (var i = 0; i < Renderer.Count; ++i)
            {
                var src = Renderer[i];
                var dst = new RendererProperty(src.ObjectType,
                    (coord != null) ? coord.Value : src.CoordinateIndex,
                    (slot != null) ? slot.Value : src.Slot,
                    src.RendererName, src.Property, src.Value, src.ValueOriginal);
                if (ME_Common.Dump_Save) Settings.Logger.LogDebug($"Save Renderer[{i}]({dst.CoordinateIndex}-{dst.Slot}) for {saver.Caption}");
                saver.Renderer.Add(dst);
            }

            for (var i = 0; i < Color.Count; ++i)
            {
                var src = Color[i];
                var dst = new MaterialColorProperty(src.ObjectType,
                    (coord != null) ? coord.Value : src.CoordinateIndex,
                    (slot != null) ? slot.Value : src.Slot, 
                    src.MaterialName, src.Property, src.Value, src.ValueOriginal);
                if (ME_Common.Dump_Save) Settings.Logger.LogDebug($"Save Color[{i}]({dst.CoordinateIndex}-{dst.Slot}) for {saver.Caption}");
                saver.Color.Add(dst);
            }

            for (var i = 0; i < Float.Count; ++i)
            {
                var src = Float[i];
                var dst = new MaterialFloatProperty(src.ObjectType,
                    (coord != null) ? coord.Value : src.CoordinateIndex,
                    (slot != null) ? slot.Value : src.Slot, 
                    src.MaterialName, src.Property, src.Value, src.ValueOriginal);
                if (ME_Common.Dump_Save) Settings.Logger.LogDebug($"Save Float[{i}]({dst.CoordinateIndex}-{dst.Slot}) for {saver.Caption}");
                saver.Float.Add(dst);
            }
        }
    }

    public class ME_ClothProps : ME_MaterialProps
    {
        public ME_ClothProps(ME_TexturePool pool, string caption)
            : base(pool, caption)
        {
        }

        //! add texture property 
        public void AddTextureProp(MaterialTextureProperty prop)
        {
            AttachTexture(prop);
        }

        //! add shader property 
        public void AddShaderProp(MaterialShader prop)
        {
            Shader.Add(prop);
        }

        //! add renderer property 
        public void AddRendererProp(RendererProperty prop)
        {
            Renderer.Add(prop);
        }

        //! add color property 
        public void AddColorProp(MaterialColorProperty prop)
        {
            Color.Add(prop);
        }

        //! add float property 
        public void AddFloatProp(MaterialFloatProperty prop)
        {
            Float.Add(prop);
        }
    }

    public class ME_AccessoryProps : ME_MaterialProps
    {
        public ME_AccessoryProps(ME_TexturePool pool, string caption)
            : base(pool, caption)
        {
        }

        //! add texture property 
        public void AddTextureProp(MaterialTextureProperty prop)
        {
            AttachTexture(prop);
        }

        //! add shader property 
        public void AddShaderProp(MaterialShader prop)
        {
            Shader.Add(prop);
        }

        //! add renderer property 
        public void AddRendererProp(RendererProperty prop)
        {
            Renderer.Add(prop);
        }

        //! add color property 
        public void AddColorProp(MaterialColorProperty prop)
        {
            Color.Add(prop);
        }

        //! add float property 
        public void AddFloatProp(MaterialFloatProperty prop)
        {
            Float.Add(prop);
        }
    }

    public class ME_CoordProps: ME_MaterialProps, IClassifiable
    {
        public ChaFileCoordinate Source { get; private set; }
        public Dictionary<int, ME_ClothProps> Cloth = new Dictionary<int, ME_ClothProps>();
        public Dictionary<int, ME_AccessoryProps> Accessory = new Dictionary<int, ME_AccessoryProps>();

        //! for chara internal coord 
        public ME_CoordProps(ChaFileControl chaFile, int idx, ME_TexturePool pool)
            : base(pool, chaFile.parameter.fullname + "-" + idx)
        {
            Settings.Logger.LogDebug($"ME_CoordProps({Caption})");
            Source = chaFile.coordinate[idx];
        }

        //! from coord card 
        public ME_CoordProps(ChaFileCoordinate coordFile, string caption="")
            : base(new ME_TexturePool(),(caption!="")?caption:(coordFile==null)?"(empty)": coordFile.coordinateName)
        {
            if (coordFile == null)
            {
                Settings.Logger.LogWarning($"ME_CoordProps(empty)");
                return;
            }

            Settings.Logger.LogDebug($"ME_CoordProps({Caption})");
            Source = coordFile;

            var loader = new ME_Loader(this);
            var data = ExtendedSave.GetExtendedDataById(coordFile, ME_Common.ExtendedDataName);
            ME_Common.Classify(loader, data);
        }

#if false
       public void Clear()
        {
            Cloth.Clear();
            Accessory.Clear();
        }
#endif

        //! get by a cloth  
        public ME_ClothProps GetClothProps(int idx, bool force)
        {
            Cloth.TryGetValue(idx, out var ret);
            if (force && ret == null)
            {
                Cloth[idx] = ret = new ME_ClothProps(_pool, Caption + "-" + idx);
            }
            return ret;
        }

        //! get by an accessory 
        public ME_AccessoryProps GetAccessoryProps(int idx, bool force)
        {
            Accessory.TryGetValue(idx, out var ret);
            if (force && ret == null)
            {
                Accessory[idx] = ret = new ME_AccessoryProps(_pool, Caption + "-" + idx);
            }
            return ret;
        }

        //! remove cloth properties 
        public void RemoveClothProps(int idx)
        {
            var prop = GetClothProps(idx, false);
            if (prop == null) return;
            if(ME_Common.Dump_Merge) Settings.Logger.LogDebug($"RemoveClothProps({idx})");
            prop.DetachTextures();
            Cloth.Remove(idx);
        }

        //! remove accessory properties 
        public void RemoveAccessoryProps(int idx)
        {
            var prop = GetAccessoryProps(idx, false);
            if (prop == null) return;
            if (ME_Common.Dump_Merge) Settings.Logger.LogDebug($"RemoveAccessoryProps({idx})");
            prop.DetachTextures();
            Accessory.Remove(idx);
        }

        //! replace cloth properties 
        public void SetClothProps(int? coord, int idx, ME_ClothProps src)
        {
            RemoveClothProps(idx);

            if (src == null) return;
            if (ME_Common.Dump_Merge) Settings.Logger.LogDebug($"SetClothProps({coord},{idx})");
            var prop = GetClothProps(idx, true);
            prop.Merge(src, coord, idx);
        }

        //! replace accessory properties 
        public void SetAccessoryProps(int? coord, int idx, ME_AccessoryProps src)
        {
            RemoveAccessoryProps(idx);

            if (src == null) return;
            if (ME_Common.Dump_Merge) Settings.Logger.LogDebug($"SetAccessoryProps({coord},{idx})");
            var prop = GetAccessoryProps(idx, true);
            prop.Merge(src, coord, idx);
        }

        //! add texture property 
        public void AddTextureProp(MaterialTextureProperty prop)
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
        public void AddShaderProp(MaterialShader prop)
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
        public void AddRendererProp(RendererProperty prop)
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
        public void AddColorProp(MaterialColorProperty prop)
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
        public void AddFloatProp(MaterialFloatProperty prop)
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

        internal void Save(ME_Saver saver,int? coord)
        {
            foreach (var t in Cloth) t.Value.Save(saver, coord, t.Key);
            foreach (var t in Accessory) t.Value.Save(saver, coord, t.Key);
        }

        public PluginData Export(bool cleanup)
        {
            var saver = new ME_Saver(Caption);
            _pool.Save(saver, cleanup);
            Save(saver, null);
            return saver.Pack();
        }

        public void Save(bool cleanup)
        {
            if (Source == null)
            {
                Settings.Logger.LogWarning($"no source to save material props for "+ Caption);
                return;
            }

            Settings.Logger.LogDebug($"Save Material Props for {Caption}");
            var pack = Export(cleanup);
            ExtendedSave.SetExtendedDataById(Source, ME_Common.ExtendedDataName, pack);
        }
    }

    public class ME_CharaProps : ME_MaterialProps, IClassifiable
    {
        public List<ME_CoordProps> Coord;
        public ChaFileControl Source { get; private set; }

        //! from chara card 
        public ME_CharaProps(ChaFileControl chaFile,string caption="")
            : base(new ME_TexturePool(), (caption != null) ? caption : chaFile.parameter.fullname)
        {
            Settings.Logger.LogDebug($"ME_CharaProps({Caption})");
            Source = chaFile;
            Coord = new List<ME_CoordProps>();
            for (var i = 0; i < chaFile.coordinate.Length; ++i)
            {
                // share TextureReferer between chara internal outfits 
                Coord.Add(new ME_CoordProps(chaFile, i, _pool));
            }

            var loader = new ME_Loader(this);
            var data = ExtendedSave.GetExtendedDataById(chaFile, ME_Common.ExtendedDataName);
            ME_Common.Classify(loader, data);
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
        public void AddTextureProp(MaterialTextureProperty prop)
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
        public void AddShaderProp(MaterialShader prop)
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
        public void AddRendererProp(RendererProperty prop)
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
        public void AddColorProp(MaterialColorProperty prop)
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
        public void AddFloatProp(MaterialFloatProperty prop)
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

        internal void Save(ME_Saver saver)
        {
            base.Save(saver, null, null);
            for (var i = 0; i < Coord.Count; ++i) Coord[i].Save(saver, i, null);
        }

        public PluginData Export(bool cleanup)
        {
            var saver = new ME_Saver(Caption);
            _pool.Save(saver, cleanup);
            Save(saver);
            return saver.Pack();
        }

        public void Save(bool cleanup)
        {
            if (Source == null)
            {
                Settings.Logger.LogWarning($"no source to save material props for "+ Caption);
                return;
            }

            Settings.Logger.LogDebug($"Save Material Props for {Caption}");
            var pack = Export(cleanup);

            ExtendedSave.SetExtendedDataById(Source, ME_Common.ExtendedDataName, pack);
        }
    }


#region Original Stuff
    public class ME_Support
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