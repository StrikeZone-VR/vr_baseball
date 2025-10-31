/// <summary>
/// 🎯 스트라이크 존 충돌 감지 스크립트 - 공이 닿으면 스트라이크 판정 처리
/// </summary>

using UnityEngine;
using UnityEngine.UI;

public class StrikeZone : MonoBehaviour
{
    //이거 사운드 설정 안할거임
    
    [Header("스트라이크 존 설정")]
    [SerializeField] private Transform[] zones; 
    public Vector3 zoneSize = new Vector3(0.5f, 1.0f, 0.1f);
    
    private Renderer zoneRenderer;
    private Collider zoneCollider;
    private Material zoneMaterial;
    private bool isFlashing = false;

    public System.Action<bool> OnPitchResult; // true = strike, false = ball

    [Header("Listening to events")] 
    [SerializeField] private VoidEventSO strikeEvent; //from GameManager 
    
    void Start()
    {
        //SetupStrikeZone();
    }

    //무슨 이상한 핑크색 타일 만듬  
    private void SetupStrikeZone()
    {
        // 태그 설정
        //gameObject.tag = "StrikeZone";

        // 컴포넌트 가져오기/추가
        zoneRenderer = GetComponent<Renderer>();
        if (zoneRenderer == null)
        {
            zoneRenderer = gameObject.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = CreateStrikeZoneMesh();
        }

        zoneCollider = GetComponent<Collider>();
        if (zoneCollider == null)
        {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.size = zoneSize;
            boxCollider.isTrigger = false; // 물리적 충돌로 변경!

            // Physics Material 추가 - 공이 튕기지 않고 즉시 멈추도록
            PhysicMaterial noBounceMaterial = new PhysicMaterial("StrikeZone_NoBounce");
            noBounceMaterial.bounciness = 0f;
            noBounceMaterial.dynamicFriction = 1f;
            noBounceMaterial.staticFriction = 1f;
            boxCollider.material = noBounceMaterial;
        }
        // 재질 설정
        //SetupMaterial();
    }
    private Mesh CreateStrikeZoneMesh()
    {
        Mesh mesh = new Mesh();

        // 스트라이크 존 크기에 맞는 박스 메시 생성
        Vector3[] vertices = new Vector3[8];
        Vector3 halfSize = zoneSize * 0.5f;

        // 앞면 4개 꼭짓점
        vertices[0] = new Vector3(-halfSize.x, -halfSize.y, halfSize.z);  // 왼쪽 아래 앞
        vertices[1] = new Vector3(halfSize.x, -halfSize.y, halfSize.z);   // 오른쪽 아래 앞
        vertices[2] = new Vector3(halfSize.x, halfSize.y, halfSize.z);    // 오른쪽 위 앞
        vertices[3] = new Vector3(-halfSize.x, halfSize.y, halfSize.z);   // 왼쪽 위 앞

        // 뒷면 4개 꼭짓점
        vertices[4] = new Vector3(-halfSize.x, -halfSize.y, -halfSize.z); // 왼쪽 아래 뒤
        vertices[5] = new Vector3(halfSize.x, -halfSize.y, -halfSize.z);  // 오른쪽 아래 뒤
        vertices[6] = new Vector3(halfSize.x, halfSize.y, -halfSize.z);   // 오른쪽 위 뒤
        vertices[7] = new Vector3(-halfSize.x, halfSize.y, -halfSize.z);  // 왼쪽 위 뒤

        // 삼각형 인덱스 (앞면만 표시)
        int[] triangles = new int[]
        {
            0, 2, 1, 0, 3, 2  // 앞면
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private bool IsPositionInStrikeZone(Vector3 position)
    {
        // 스트라이크 존 범위 체크
        Bounds bounds = new Bounds(transform.position, zoneSize);
        return bounds.Contains(position);
    }

    public Transform GetZone(int index)
    {
        return zones[index];
    }
}
