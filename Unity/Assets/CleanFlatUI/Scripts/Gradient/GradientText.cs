using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RainbowArt.CleanFlatUI
{
    public class GradientText : TextMeshProUGUI
    {
        [SerializeField]
        bool colorGradientLine = true;

        [SerializeField]
        Gradient gradientColors;


#if UNITY_2023_2_OR_NEWER
        protected override void FillCharacterVertexBuffers(int i)
#else
        protected override void FillCharacterVertexBuffers(int i, int indexParam)
#endif
        {
            int materialIndex = m_textInfo.characterInfo[i].materialReferenceIndex;
            int index_X4 = m_textInfo.meshInfo[materialIndex].vertexCount;

            if (index_X4 >= m_textInfo.meshInfo[materialIndex].vertices.Length)
            {
                m_textInfo.meshInfo[materialIndex].ResizeMeshInfo(Mathf.NextPowerOfTwo((index_X4 + 4) / 4));
            }

            TMP_CharacterInfo[] characterInfoArray = m_textInfo.characterInfo;
            m_textInfo.characterInfo[i].vertexIndex = index_X4;

            // Setup Vertices for Characters
            m_textInfo.meshInfo[materialIndex].vertices[0 + index_X4] = characterInfoArray[i].vertex_BL.position;
            m_textInfo.meshInfo[materialIndex].vertices[1 + index_X4] = characterInfoArray[i].vertex_TL.position;
            m_textInfo.meshInfo[materialIndex].vertices[2 + index_X4] = characterInfoArray[i].vertex_TR.position;
            m_textInfo.meshInfo[materialIndex].vertices[3 + index_X4] = characterInfoArray[i].vertex_BR.position;

            // Setup UVS0
            m_textInfo.meshInfo[materialIndex].uvs0[0 + index_X4] = characterInfoArray[i].vertex_BL.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[1 + index_X4] = characterInfoArray[i].vertex_TL.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[2 + index_X4] = characterInfoArray[i].vertex_TR.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[3 + index_X4] = characterInfoArray[i].vertex_BR.uv;
            
            // Setup UVS2
            m_textInfo.meshInfo[materialIndex].uvs2[0 + index_X4] = characterInfoArray[i].vertex_BL.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[1 + index_X4] = characterInfoArray[i].vertex_TL.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[2 + index_X4] = characterInfoArray[i].vertex_TR.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[3 + index_X4] = characterInfoArray[i].vertex_BR.uv2;
            
            // Setup UVS4
            //m_textInfo.meshInfo[0].uvs4[0 + index_X4] = characterInfoArray[i].vertex_BL.uv4;
            //m_textInfo.meshInfo[0].uvs4[1 + index_X4] = characterInfoArray[i].vertex_TL.uv4;
            //m_textInfo.meshInfo[0].uvs4[2 + index_X4] = characterInfoArray[i].vertex_TR.uv4;
            //m_textInfo.meshInfo[0].uvs4[3 + index_X4] = characterInfoArray[i].vertex_BR.uv4;
            
            // setup Vertex Colors
            m_textInfo.meshInfo[materialIndex].colors32[0 + index_X4] = characterInfoArray[i].vertex_BL.color;
            m_textInfo.meshInfo[materialIndex].colors32[1 + index_X4] = characterInfoArray[i].vertex_TL.color;
            m_textInfo.meshInfo[materialIndex].colors32[2 + index_X4] = characterInfoArray[i].vertex_TR.color;
            m_textInfo.meshInfo[materialIndex].colors32[3 + index_X4] = characterInfoArray[i].vertex_BR.color;

            m_textInfo.meshInfo[materialIndex].vertexCount = index_X4 + 4;

            if(colorGradientLine)
            {
                TMP_MeshInfo info = m_textInfo.meshInfo[materialIndex];
                float minX = info.vertices[0].x;
                float maxX = info.vertices[0].x;
                float curX = 0f;

                for (int idx = (i + 1) * 4 - 1; idx >= 1; --idx)
                {
                    curX = info.vertices[idx].x;
                    if (curX > maxX)
                    {
                        maxX = curX;
                    }
                    else if (curX < minX)
                    {
                        minX = curX;
                    }
                }
                float lineWidth = 0;
                if ((maxX - minX) > 0)
                {
                    lineWidth = 1f / (maxX - minX);
                }

                for (int idx = 0; idx < index_X4 + 4; idx++)
                {
                    Color32 c32 = gradientColors.Evaluate((info.vertices[idx].x - minX) * lineWidth);
                    m_textInfo.meshInfo[materialIndex].colors32[idx] = c32;
                }
            }
            
        }
    }
}
