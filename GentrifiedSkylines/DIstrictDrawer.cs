/*
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using AdaptiveGentrification.Redirection;


namespace AdaptiveGentrification.Detours
{
    [TargetType(typeof(CommonBuildingAI))]

    public class DistrictManagerDetour : DistrictManager
    {  
        bool m_namesModified = false;
        Mesh m_nameMesh = new Mesh();
        Mesh m_iconMesh = new Mesh();
        [RedirectMethod]
        public void UpdateNames()
        {
            UIFontManager.Invalidate(this.m_properties.m_areaNameFont);
            this.m_namesModified = false;
            UIRenderData destination = UIRenderData.Obtain();
            UIRenderData uiRenderData = UIRenderData.Obtain();
            try
            {
                destination.Clear();
                uiRenderData.Clear();
                PoolList<Vector3> vertices1 = uiRenderData.vertices;
                PoolList<Vector3> normals1 = uiRenderData.normals;
                PoolList<Color32> colors1 = uiRenderData.colors;
                PoolList<Vector2> uvs1 = uiRenderData.uvs;
                PoolList<int> triangles1 = uiRenderData.triangles;
                PoolList<Vector3> vertices2 = destination.vertices;
                PoolList<Vector3> normals2 = destination.normals;
                PoolList<Color32> colors2 = destination.colors;
                PoolList<Vector2> uvs2 = destination.uvs;
                PoolList<int> triangles2 = destination.triangles;
                for (int district = 1; district < 128; ++district)
                {
                    if (this.m_districts.m_buffer[district].m_flags != District.Flags.None)
                    {
                        string text = this.GetDistrictName(district) + "\n";
                        text = "2hu hijack lol";
                        PositionData<DistrictPolicies.Policies>[] orderedEnumData = Utils.GetOrderedEnumData<DistrictPolicies.Policies>();
                        for (int index = 0; index < orderedEnumData.Length; ++index)
                        {
                            if (this.IsDistrictPolicySet(orderedEnumData[index].enumValue, (byte)district))
                            {
                                string str = "IconPolicy" + orderedEnumData[index].enumName;
                                text = text + "<sprite " + str + "> ";
                            }
                        }
                        if (text != null)
                        {
                            int count1 = normals2.Count;
                            int count2 = normals1.Count;
                            using (UIFontRenderer renderer = this.m_properties.m_areaNameFont.ObtainRenderer())
                            {
                                UIDynamicFont.DynamicFontRenderer dynamicFontRenderer = renderer as UIDynamicFont.DynamicFontRenderer;
                                if (dynamicFontRenderer != null)
                                {
                                    dynamicFontRenderer.spriteAtlas = this.m_properties.m_areaIconAtlas;
                                    dynamicFontRenderer.spriteBuffer = uiRenderData;
                                }
                                renderer.defaultColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)64);
                                renderer.textScale = 2f;
                                renderer.pixelRatio = 1f;
                                renderer.processMarkup = true;
                                renderer.multiLine = true;
                                renderer.wordWrap = true;
                                renderer.textAlign = UIHorizontalAlignment.Center;
                                renderer.maxSize = new Vector2(450f, 900f);
                                renderer.shadow = false;
                                renderer.shadowColor = (Color32)Color.black;
                                renderer.shadowOffset = Vector2.one;
                                Vector2 vector2 = renderer.MeasureString(text);
                                this.m_districts.m_buffer[district].m_nameSize = vector2;
                                vertices2.Add(new Vector3(-vector2.x, -vector2.y, 1f));
                                vertices2.Add(new Vector3(-vector2.x, vector2.y, 1f));
                                vertices2.Add(new Vector3(vector2.x, vector2.y, 1f));
                                vertices2.Add(new Vector3(vector2.x, -vector2.y, 1f));
                                colors2.Add(new Color32((byte)0, (byte)0, (byte)0, byte.MaxValue));
                                colors2.Add(new Color32((byte)0, (byte)0, (byte)0, byte.MaxValue));
                                colors2.Add(new Color32((byte)0, (byte)0, (byte)0, byte.MaxValue));
                                colors2.Add(new Color32((byte)0, (byte)0, (byte)0, byte.MaxValue));
                                uvs2.Add(new Vector2(-1f, -1f));
                                uvs2.Add(new Vector2(-1f, 1f));
                                uvs2.Add(new Vector2(1f, 1f));
                                uvs2.Add(new Vector2(1f, -1f));
                                triangles2.Add(vertices2.Count - 4);
                                triangles2.Add(vertices2.Count - 3);
                                triangles2.Add(vertices2.Count - 1);
                                triangles2.Add(vertices2.Count - 1);
                                triangles2.Add(vertices2.Count - 3);
                                triangles2.Add(vertices2.Count - 2);
                                renderer.vectorOffset = new Vector3(-225f, vector2.y * 0.5f, 0.0f);
                                renderer.Render(text, destination);
                            }
                            int count3 = vertices2.Count;
                            int count4 = normals2.Count;
                            Vector3 vector3 = this.m_districts.m_buffer[district].m_nameLocation;
                            for (int index = count1; index < count4; ++index)
                                normals2[index] = vector3;
                            for (int index = count4; index < count3; ++index)
                                normals2.Add(vector3);
                            int count5 = vertices1.Count;
                            int count6 = normals1.Count;
                            for (int index = count2; index < count6; ++index)
                                normals1[index] = vector3;
                            for (int index = count6; index < count5; ++index)
                                normals1.Add(vector3);
                        }
                    }
                }
                if ((UnityEngine.Object)this.m_nameMesh == (UnityEngine.Object)null)
                    this.m_nameMesh = new Mesh();
                this.m_nameMesh.Clear();
                this.m_nameMesh.vertices = vertices2.ToArray();
                this.m_nameMesh.normals = normals2.ToArray();
                this.m_nameMesh.colors32 = colors2.ToArray();
                this.m_nameMesh.uv = uvs2.ToArray();
                this.m_nameMesh.triangles = triangles2.ToArray();
                this.m_nameMesh.bounds = new Bounds(Vector3.zero, new Vector3(9830.4f, 1024f, 9830.4f));
                if ((UnityEngine.Object)this.m_iconMesh == (UnityEngine.Object)null)
                    this.m_iconMesh = new Mesh();
                this.m_iconMesh.Clear();
                this.m_iconMesh.vertices = vertices1.ToArray();
                this.m_iconMesh.normals = normals1.ToArray();
                this.m_iconMesh.colors32 = colors1.ToArray();
                this.m_iconMesh.uv = uvs1.ToArray();
                this.m_iconMesh.triangles = triangles1.ToArray();
                this.m_iconMesh.bounds = new Bounds(Vector3.zero, new Vector3(9830.4f, 1024f, 9830.4f));
            }
            finally
            {
                destination.Release();
                uiRenderData.Release();
            }
        }

    }
}
*/