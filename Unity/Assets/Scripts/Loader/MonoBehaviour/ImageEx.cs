using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace ET.UI
{
    /// <summary>
    /// 扩展UI.Image
    /// Filled下，Sliced依然生效
    /// </summary>
    public class ImageEx : Image
    {
        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            if (hasBorder && type == Type.Filled && (fillMethod == FillMethod.Horizontal || fillMethod == FillMethod.Vertical))
            {
                GenerateSlicedFilledSprite(toFill);
            }
            else
            {
                base.OnPopulateMesh(toFill);
            }
        }

        Vector4 GetAdjustedBorders(Vector4 border, Rect rect)
        {
            for (int axis = 0; axis <= 1; axis++)
            {
                // If the rect is smaller than the combined borders, then there's not room for the borders at their normal size.
                // In order to avoid artefacts with overlapping borders, we scale the borders down to fit.
                float combinedBorders = border[axis] + border[axis + 2];
                if (rect.size[axis] < combinedBorders && combinedBorders != 0)
                {
                    float borderScaleRatio = rect.size[axis] / combinedBorders;
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }
            }
            return border;
        }

        private void GenerateSlicedFilledSprite(VertexHelper toFill)
        {
            toFill.Clear();

            if (fillAmount < 0.001f)
                return;

            Rect rect = GetPixelAdjustedRect();
            Vector4 outer = DataUtility.GetOuterUV(overrideSprite);
            Vector4 padding = DataUtility.GetPadding(overrideSprite);

            Vector4 inner = DataUtility.GetInnerUV(overrideSprite);
            Vector4 border = GetAdjustedBorders(overrideSprite.border / pixelsPerUnit, rect);

            padding = padding / pixelsPerUnit;

            s_SlicedVertices[0] = new Vector2(padding.x, padding.y);
            s_SlicedVertices[3] = new Vector2(rect.width - padding.z, rect.height - padding.w);

            s_SlicedVertices[1].x = border.x;
            s_SlicedVertices[1].y = border.y;

            s_SlicedVertices[2].x = rect.width - border.z;
            s_SlicedVertices[2].y = rect.height - border.w;

            for (int i = 0; i < 4; ++i)
            {
                s_SlicedVertices[i].x += rect.x;
                s_SlicedVertices[i].y += rect.y;
            }

            s_SlicedUVs[0] = new Vector2(outer.x, outer.y);
            s_SlicedUVs[1] = new Vector2(inner.x, inner.y);
            s_SlicedUVs[2] = new Vector2(inner.z, inner.w);
            s_SlicedUVs[3] = new Vector2(outer.z, outer.w);

            float rectStartPos;
            float _1OverTotalSize;
            if (fillMethod == FillMethod.Horizontal)
            {
                rectStartPos = s_SlicedVertices[0].x;

                float totalSize = (s_SlicedVertices[3].x - s_SlicedVertices[0].x);
                _1OverTotalSize = totalSize > 0f ? 1f / totalSize : 1f;
            }
            else
            {
                rectStartPos = s_SlicedVertices[0].y;

                float totalSize = (s_SlicedVertices[3].y - s_SlicedVertices[0].y);
                _1OverTotalSize = totalSize > 0f ? 1f / totalSize : 1f;
            }

            for (int x = 0; x < 3; x++)
            {
                int x2 = x + 1;

                for (int y = 0; y < 3; y++)
                {
                    if (!fillCenter && x == 1 && y == 1)
                        continue;

                    int y2 = y + 1;


                    float sliceStart = 0f, sliceEnd = 0f;
                    if (fillMethod == FillMethod.Horizontal)
                    {
                        if (fillOrigin == (int)OriginHorizontal.Left)
                        {
                            sliceStart = (s_SlicedVertices[x].x - rectStartPos) * _1OverTotalSize;
                            sliceEnd = (s_SlicedVertices[x2].x - rectStartPos) * _1OverTotalSize;
                        }
                        else if (fillOrigin == (int)OriginHorizontal.Right)
                        {
                            sliceStart = 1f - (s_SlicedVertices[x2].x - rectStartPos) * _1OverTotalSize;
                            sliceEnd = 1f - (s_SlicedVertices[x].x - rectStartPos) * _1OverTotalSize;
                        }
                    }
                    else if (fillMethod == FillMethod.Vertical)
                    {
                        if (fillOrigin == (int)OriginVertical.Top)
                        {
                            sliceStart = 1f - (s_SlicedVertices[y2].y - rectStartPos) * _1OverTotalSize;
                            sliceEnd = 1f - (s_SlicedVertices[y].y - rectStartPos) * _1OverTotalSize;
                        }
                        else if (fillOrigin == (int)OriginVertical.Bottom)
                        {
                            sliceStart = (s_SlicedVertices[y].y - rectStartPos) * _1OverTotalSize;
                            sliceEnd = (s_SlicedVertices[y2].y - rectStartPos) * _1OverTotalSize;
                        }
                    }

                    if (sliceStart >= fillAmount)
                        continue;

                    Vector4 vertices = new Vector4(s_SlicedVertices[x].x, s_SlicedVertices[y].y, s_SlicedVertices[x2].x, s_SlicedVertices[y2].y);
                    Vector4 uvs = new Vector4(s_SlicedUVs[x].x, s_SlicedUVs[y].y, s_SlicedUVs[x2].x, s_SlicedUVs[y2].y);
                    float fillAmount_ = (fillAmount - sliceStart) / (sliceEnd - sliceStart);

                    GenerateFilledSprite(toFill, vertices, uvs, fillAmount_);
                }
            }
        }

        private void GenerateFilledSprite(VertexHelper vh, Vector4 vertices, Vector4 uvs, float fillAmount_)
        {
            float uvLeft = uvs.x;
            float uvBottom = uvs.y;
            float uvRight = uvs.z;
            float uvTop = uvs.w;

            if (fillAmount_ < 1f)
            {
                if (fillMethod == FillMethod.Horizontal)
                {
                    if (fillOrigin == (int)OriginHorizontal.Left)
                    {
                        vertices.z = vertices.x + (vertices.z - vertices.x) * fillAmount_;
                        uvRight = uvLeft + (uvRight - uvLeft) * fillAmount_;
                    }
                    else if (fillOrigin == (int)OriginHorizontal.Right)
                    {
                        vertices.x = vertices.z - (vertices.z - vertices.x) * fillAmount_;
                        uvLeft = uvRight - (uvRight - uvLeft) * fillAmount_;
                    }
                }
                else if (fillMethod == FillMethod.Vertical)
                {
                    if (fillOrigin == (int)OriginVertical.Bottom)
                    {
                        vertices.w = vertices.y + (vertices.w - vertices.y) * fillAmount_;
                        uvTop = uvBottom + (uvTop - uvBottom) * fillAmount_;
                    }
                    else if (fillOrigin == (int)OriginVertical.Top)
                    {
                        vertices.y = vertices.w - (vertices.w - vertices.y) * fillAmount_;
                        uvBottom = uvTop - (uvTop - uvBottom) * fillAmount_;
                    }
                }
            }

            s_Vertices[0] = new Vector3(vertices.x, vertices.y);
            s_Vertices[1] = new Vector3(vertices.x, vertices.w);
            s_Vertices[2] = new Vector3(vertices.z, vertices.w);
            s_Vertices[3] = new Vector3(vertices.z, vertices.y);

            s_UVs[0] = new Vector2(uvLeft, uvBottom);
            s_UVs[1] = new Vector2(uvLeft, uvTop);
            s_UVs[2] = new Vector2(uvRight, uvTop);
            s_UVs[3] = new Vector2(uvRight, uvBottom);

            int startIndex = vh.currentVertCount;

            for (int i = 0; i < 4; i++)
                vh.AddVert(s_Vertices[i], color, s_UVs[i]);

            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }

        private static readonly Vector3[] s_Vertices = new Vector3[4];
        private static readonly Vector2[] s_UVs = new Vector2[4];
        private static readonly Vector2[] s_SlicedVertices = new Vector2[4];
        private static readonly Vector2[] s_SlicedUVs = new Vector2[4];
    }
}