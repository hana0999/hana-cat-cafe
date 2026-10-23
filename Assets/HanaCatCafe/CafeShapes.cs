using System.Collections.Generic;
using UnityEngine;

namespace Hana.CatCafe
{
    // Reusable low-gloss shapes. Generated materials are owned and disposed by the controller.
    public sealed class CafeShapes
    {
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        public static Color Hex(string s) { Color c; ColorUtility.TryParseHtmlString(s, out c); return c; }
        public Material Material(Color color)
        {
            if (materials.TryGetValue(color, out var result)) return result;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null || shader == null)
                shader = Shader.Find("Standard");
            result = new Material(shader);
            result.color = color;
            if (result.HasProperty("_Smoothness")) result.SetFloat("_Smoothness", 0.06f);
            if (result.HasProperty("_Glossiness")) result.SetFloat("_Glossiness", 0.06f);
            materials[color] = result;
            return result;
        }
        public Transform Shape(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            Object.Destroy(go.GetComponent<Collider>());
            return go.transform;
        }
        public Transform Ball(Transform p, string name, Vector3 at, Vector3 size, Color c)
            => Shape(p, name, PrimitiveType.Sphere, at, size, c);
        public Transform Box(Transform p, string name, Vector3 at, Vector3 size, Color c)
            => Shape(p, name, PrimitiveType.Cube, at, size, c);
        public Transform Disc(Transform p, string name, Vector3 at, float radius, float height, Color c)
            => Shape(p, name, PrimitiveType.Cylinder, at, new Vector3(radius * 2, height / 2, radius * 2), c);
        public Transform Group(Transform parent, string name, Vector3 at)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = at;
            return go.transform;
        }
        public void Dispose() { foreach (var m in materials.Values) Object.Destroy(m); materials.Clear(); }
    }
}
