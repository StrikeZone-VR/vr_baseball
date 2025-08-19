/// <summary>
/// 🎯 스트라이크 존 충돌 감지 스크립트 - 공이 닿으면 스트라이크 판정 처리
/// </summary>

using UnityEngine;
using UnityEngine.UI;

public class StrikeZone : MonoBehaviour
{
    [Header("스트라이크 존 설정")]
    public Vector3 zoneSize = new Vector3(0.5f, 1.0f, 0.1f);
    public Color normalColor = Color.white;
    public Color strikeColor = Color.green;
    public Color ballColor = Color.red;

    [Header("시각적 피드백")]
    public float flashDuration = 0.5f;
    public ParticleSystem strikeEffect;
    public ParticleSystem ballEffect;

    [Header("오디오")]
    public AudioClip strikeSound;
    public AudioClip ballSound;

    private Renderer zoneRenderer;
    private AudioSource audioSource;
    private Collider zoneCollider;
    private Material zoneMaterial;
    private Color originalColor;
    private bool isFlashing = false;

    public System.Action<bool> OnPitchResult; // true = strike, false = ball

    void Start()
    {
        SetupStrikeZone();
    }

    private void SetupStrikeZone()
    {
        // 태그 설정
        gameObject.tag = "StrikeZone";

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

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // 재질 설정
        SetupMaterial();
    }

    private void SetupMaterial()
    {
        if (zoneRenderer != null)
        {
            zoneMaterial = new Material(Shader.Find("Standard"));
            zoneMaterial.color = normalColor;
            zoneMaterial.SetFloat("_Mode", 3); // Transparent mode
            zoneMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            zoneMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            zoneMaterial.SetInt("_ZWrite", 0);
            zoneMaterial.DisableKeyword("_ALPHATEST_ON");
            zoneMaterial.EnableKeyword("_ALPHABLEND_ON");
            zoneMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            zoneMaterial.renderQueue = 3000;

            Color transparentColor = normalColor;
            transparentColor.a = 0.3f; // 반투명
            zoneMaterial.color = transparentColor;
            originalColor = transparentColor;

            zoneRenderer.material = zoneMaterial;
        }
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

    void OnTriggerEnter(Collider other)
    {
        VRBaseball baseball = other.GetComponent<VRBaseball>();
        if (baseball != null)
        {
            HandleStrike(baseball);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // 공이 스트라이크 존 근처에 떨어졌을 때
        VRBaseball baseball = collision.gameObject.GetComponent<VRBaseball>();
        if (baseball != null)
        {
            // 공의 위치가 스트라이크 존 범위 내인지 확인
            Vector3 ballPosition = collision.contacts[0].point;
            bool isInStrikeZone = IsPositionInStrikeZone(ballPosition);

            if (isInStrikeZone)
            {
                HandleStrike(baseball);
            }
            else
            {
                HandleBall(baseball);
            }
        }
    }

    private bool IsPositionInStrikeZone(Vector3 position)
    {
        // 스트라이크 존 범위 체크
        Bounds bounds = new Bounds(transform.position, zoneSize);
        return bounds.Contains(position);
    }

    private void HandleStrike(VRBaseball baseball)
    {
        Debug.Log("스트라이크!");

        FlashZone(strikeColor);

        if (strikeEffect != null)
            strikeEffect.Play();

        if (audioSource != null && strikeSound != null)
            audioSource.PlayOneShot(strikeSound);

        OnPitchResult?.Invoke(true);
    }

    private void HandleBall(VRBaseball baseball)
    {
        Debug.Log("볼!");

        FlashZone(ballColor);

        if (ballEffect != null)
            ballEffect.Play();

        if (audioSource != null && ballSound != null)
            audioSource.PlayOneShot(ballSound);

        OnPitchResult?.Invoke(false);
    }

    private void FlashZone(Color flashColor)
    {
        if (isFlashing || zoneMaterial == null) return;

        isFlashing = true;
        StartCoroutine(FlashCoroutine(flashColor));
    }

    private System.Collections.IEnumerator FlashCoroutine(Color flashColor)
    {
        Color targetColor = flashColor;
        targetColor.a = 0.7f; // 더 불투명하게

        // 색상 변경
        zoneMaterial.color = targetColor;

        yield return new WaitForSeconds(flashDuration);

        // 원래 색상으로 복원
        zoneMaterial.color = originalColor;
        isFlashing = false;
    }

    // 에디터에서 기즈모 그리기
    void OnDrawGizmos()
    {
        Gizmos.color = normalColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, zoneSize);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, zoneSize);
    }
}
