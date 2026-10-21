using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Tạo lại tài nguyên hiệu ứng nổ và gán cấu hình cho ba loại thiên thạch trong Unity Editor.</summary>
public static class AsteroidExplosionAssets
{
    public const string PrefabPath = "Assets/Level/Prefabs/Enemy/Asteroid_Explosion.prefab";
    public const string AtlasPath = "Assets/Art/Sprites/Enemy/Asteroids/Asteroid_Explosion_Atlas.png";

    /// <summary>Tạo vật liệu, bốn hệ hạt và prefab vụ nổ, sau đó gán hiệu ứng cho các prefab thiên thạch.</summary>
    [MenuItem("Tools/Space Explorer/Rebuild Asteroid Explosion Assets")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
        if (importer == null) throw new FileNotFoundException("Missing transparent explosion atlas", AtlasPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        var shader = Shader.Find("Space Explorer/Asteroid Particle");
        if (shader == null) throw new System.InvalidOperationException("Explosion shader did not compile");
        Directory.CreateDirectory("Assets/Art/Materials/Asteroids");
        AssetDatabase.Refresh();
        var root = new GameObject("Asteroid_Explosion");
        try
        {
            var effect = root.AddComponent<AsteroidExplosion>();
            var flash = Create(root, "Flash", Material("Flash", texture, shader, new Vector2(0, .5f)),
                1, .18f, .18f, .72f, .72f, 0, 0, 0, false);
            var fragments = Create(root, "Fragments", Material("Fragments", texture, shader, new Vector2(.5f, .5f)),
                7, .55f, .95f, .09f, .17f, .65f, 1.5f, .07f, true);
            var sparks = Create(root, "Sparks", Material("Sparks", texture, shader, new Vector2(0, 0)),
                12, .2f, .48f, .07f, .16f, 1.2f, 2.4f, .04f, false);
            var dust = Create(root, "Dust", Material("Dust", texture, shader, new Vector2(.5f, 0)),
                4, .4f, .72f, .24f, .38f, .2f, .5f, .08f, true);
            var serialized = new SerializedObject(effect);
            serialized.FindProperty("flash").objectReferenceValue = flash;
            serialized.FindProperty("fragments").objectReferenceValue = fragments;
            serialized.FindProperty("sparks").objectReferenceValue = sparks;
            serialized.FindProperty("dust").objectReferenceValue = dust;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath).GetComponent<AsteroidExplosion>();
            Assign("Small", prefab, 7, 12, Color.white);
            // Thiên thạch vừa có màu xám, nên giảm sắc ấm của mảnh vỡ để hợp với sprite gốc.
            Assign("Medium", prefab, 12, 20, new Color(.86f, .86f, .8f, 1));
            Assign("Large", prefab, 20, 32, Color.white);
            AssetDatabase.SaveAssets();
        }
        finally { Object.DestroyImmediate(root); }
    }

    /// <summary>Tạo hoặc cập nhật vật liệu để lấy một ô trong ảnh atlas 2 × 2.</summary>
    /// <param name="name">Tên phần hiệu ứng, dùng để đặt tên file vật liệu.</param>
    /// <param name="texture">Ảnh atlas chứa các phần hiệu ứng.</param>
    /// <param name="shader">Shader dùng để vẽ hạt.</param>
    /// <param name="offset">Độ lệch UV xác định ô cần lấy trong atlas.</param>
    /// <returns>Vật liệu đã được lưu và cấu hình.</returns>
    static Material Material(string name, Texture texture, Shader shader, Vector2 offset)
    {
        string path = "Assets/Art/Materials/Asteroids/Explosion_" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.mainTexture = texture;
        // Mỗi phần hiệu ứng chiếm một ô bằng nửa chiều rộng và chiều cao của atlas 2 × 2.
        material.mainTextureScale = Vector2.one * .5f;
        material.mainTextureOffset = offset;
        EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>Tạo hệ hạt con phát một đợt, cấu hình chuyển động, độ mờ và kích thước theo thời gian.</summary>
    /// <param name="root">Đối tượng cha của hiệu ứng.</param>
    /// <param name="name">Tên hệ hạt, cũng quyết định một số kiểu hiển thị.</param>
    /// <param name="material">Vật liệu vẽ hạt.</param>
    /// <param name="count">Số hạt phát trong đợt.</param>
    /// <param name="lifeMin">Thời gian sống ngắn nhất, tính bằng giây.</param>
    /// <param name="lifeMax">Thời gian sống dài nhất, tính bằng giây.</param>
    /// <param name="sizeMin">Kích thước ban đầu nhỏ nhất.</param>
    /// <param name="sizeMax">Kích thước ban đầu lớn nhất.</param>
    /// <param name="speedMin">Tốc độ ban đầu thấp nhất.</param>
    /// <param name="speedMax">Tốc độ ban đầu cao nhất.</param>
    /// <param name="radius">Bán kính vùng phát hạt tròn.</param>
    /// <param name="spin">Có bật xoay hạt trong thời gian sống hay không.</param>
    /// <returns>Hệ hạt vừa được tạo.</returns>
    static ParticleSystem Create(GameObject root, string name, Material material, int count,
        float lifeMin, float lifeMax, float sizeMin, float sizeMax, float speedMin, float speedMax,
        float radius, bool spin)
    {
        var child = new GameObject(name);
        child.transform.SetParent(root.transform, false);
        var system = child.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.duration = .05f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 128;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.gravityModifier = name == "Fragments" ? .035f : 0;
        var emission = system.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)count) });
        var shape = system.shape;
        shape.enabled = radius > 0;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.arc = 360;
        var color = system.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .3f), new GradientAlphaKey(0, 1) });
        color.color = gradient;
        var size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1, name == "Flash" || name == "Dust"
            ? AnimationCurve.EaseInOut(0, .45f, 1, 1.6f) : AnimationCurve.Linear(0, 1, 1, .35f));
        if (spin)
        {
            var rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-5, 5);
        }
        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = name == "Flash" || name == "Sparks" ? 2 : 1;
        renderer.minParticleSize = 0;
        renderer.maxParticleSize = 1;
        return system;
    }

    /// <summary>Mở prefab thiên thạch, gán hiệu ứng và các thông số riêng rồi lưu lại.</summary>
    /// <param name="name">Loại thiên thạch: Small, Medium hoặc Large.</param>
    /// <param name="prefab">Prefab hiệu ứng nổ dùng chung.</param>
    /// <param name="fragments">Số mảnh vỡ.</param>
    /// <param name="sparks">Số tia lửa.</param>
    /// <param name="tint">Màu phủ lên mảnh vỡ.</param>
    static void Assign(string name, AsteroidExplosion prefab, int fragments, int sparks, Color tint)
    {
        string path = "Assets/Level/Prefabs/Enemy/Asteroid_" + name + ".prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var effect = root.GetComponent<AsteroidDeathVfx>();
            if (effect == null) effect = root.AddComponent<AsteroidDeathVfx>();
            var serialized = new SerializedObject(effect);
            serialized.FindProperty("explosionPrefab").objectReferenceValue = prefab;
            serialized.FindProperty("fragmentCount").intValue = fragments;
            serialized.FindProperty("sparkCount").intValue = sparks;
            serialized.FindProperty("fragmentTint").colorValue = tint;
            serialized.FindProperty("fragmentSaturation").floatValue = name == "Medium" ? .05f : 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
