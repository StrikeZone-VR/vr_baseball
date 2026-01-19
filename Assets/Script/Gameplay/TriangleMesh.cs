using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class TriangleMesh : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Mesh mesh  = new Mesh();

        float width = 0.43f;
        float height = 0.1f;
        Vector3[] vertices = new Vector3[10];
        //Vector2[] uv = new Vector2[10];
        int[] triangles = new int[48];
        
        vertices[0] = new Vector3(0, 0, 0);
        vertices[1] = new Vector3(width, 0, 0);
        vertices[2] = new Vector3(0, 0, width);
        vertices[3] = new Vector3(width, 0, width);
        vertices[4] = new Vector3(Mathf.Sqrt(width), 0, width + Mathf.Sqrt(width));
        vertices[5] = new Vector3(0, height, 0);
        vertices[6] = new Vector3(width, height, 0);
        vertices[7] = new Vector3(0, height, width);
        vertices[8] = new Vector3(width, height, width);
        vertices[9] = new Vector3(Mathf.Sqrt(width), height, width + Mathf.Sqrt(width));
        
        //uv
        
        
        //밑면 => {0, 4, 2}, {0, 1, 4}, {1, 3, 4} 
        triangles[0] = 0;
        triangles[1] = 4;
        triangles[2] = 2; 
        triangles[3] = 0;
        triangles[4] = 1;
        triangles[5] = 4; 
        triangles[6] = 1;
        triangles[7] = 3;
        triangles[8] = 4;
        
        //옆면
        triangles[9] = 5;
        triangles[10] = 2;
        triangles[11] = 0; 
        triangles[12] = 5;
        triangles[13] = 7;
        triangles[14] = 2;
        
        //옆면
        triangles[15] = 5;
        triangles[16] = 1;
        triangles[17] = 0; 
        triangles[18] = 5;
        triangles[19] = 6;
        triangles[20] = 1;
        
        //옆면
        triangles[21] = 6;
        triangles[22] = 3;
        triangles[23] = 1; 
        triangles[24] = 6;
        triangles[25] = 8;
        triangles[26] = 3;
        
        //옆면
        triangles[27] = 8;
        triangles[28] = 3;
        triangles[29] = 4; 
        triangles[30] = 8;
        triangles[31] = 4;
        triangles[32] = 9;
        
        //옆면
        triangles[33] = 7;
        triangles[34] = 4;
        triangles[35] = 2; 
        triangles[36] = 8;
        triangles[37] = 9;
        triangles[38] = 4;
        
        //윗면 => {0, 4, 2}, {0, 1, 4}, {1, 3, 4} 
        triangles[39] = 5;
        triangles[40] = 9;
        triangles[41] = 7; 
        triangles[42] = 5;
        triangles[43] = 6;
        triangles[44] = 9; 
        triangles[45] = 6;
        triangles[46] = 8;
        triangles[47] = 9;
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;        //저 triangle이 index버퍼임
        
        GetComponent<MeshFilter>().mesh = mesh;
    }
}
