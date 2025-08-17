using UnityEngine;
using System.Collections;

/// <summary>
/// ⚾ 볼 존 스크립트
/// 공이 닿으면 볼 판정 처리
/// </summary>
public class BallZone : MonoBehaviour
{
    [Header("⚾ 볼존 설정")]
    public Color ballColor = Color.red;
    public float flashDuration = 0.5f;

    [Header("오디오")]
    public AudioClip ballSound;

    private UnifiedZoneManager zoneManager;
    private Renderer zoneRenderer;
    private AudioSource audioSource;
    private Material originalMaterial;
    private bool isFlashing = false;

    // ==============================================
    // 🏗️ 초기화
    // ==============================================
    public void SetupBallZone(UnifiedZoneManager manager)
    {
        zoneManager = manager;

        // 컴포넌트 설정
        zoneRenderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (zoneRenderer != null)
        {
            originalMaterial = zoneRenderer.material;
        }

        // 태그 설정
        gameObject.tag = "BallZone";
    }

    // ==============================================
    // 🎯 충돌 처리
    // ==============================================
    void OnTriggerEnter(Collider other)
    {
        VRBaseball baseball = other.GetComponent<VRBaseball>();
        if (baseball != null)
        {
            HandleBallHit(baseball);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        VRBaseball baseball = collision.gameObject.GetComponent<VRBaseball>();
        if (baseball != null)
        {
            HandleBallHit(baseball);
        }
    }

    // ==============================================
    // 🎾 볼 처리
    // ==============================================
    private void HandleBallHit(VRBaseball baseball)
    {
        Debug.Log($"❌ 볼! 위치: {transform.name}");

        // 시각적 피드백
        FlashZone();

        // 오디오 피드백
        if (audioSource != null && ballSound != null)
        {
            audioSource.PlayOneShot(ballSound);
        }

        // 매니저에 알림
        if (zoneManager != null)
        {
            zoneManager.HandleBallHit(transform.position, baseball);
        }
    }

    // ==============================================
    // 🎨 시각적 피드백
    // ==============================================
    private void FlashZone()
    {
        if (!isFlashing && zoneRenderer != null)
        {
            StartCoroutine(FlashCoroutine());
        }
    }

    private System.Collections.IEnumerator FlashCoroutine()
    {
        isFlashing = true;

        // 색상 변경
        if (zoneRenderer != null)
        {
            Material flashMaterial = new Material(originalMaterial);
            flashMaterial.color = ballColor;
            zoneRenderer.material = flashMaterial;
        }

        yield return new WaitForSeconds(flashDuration);

        // 원래 색상으로 복원
        if (zoneRenderer != null && originalMaterial != null)
        {
            zoneRenderer.material = originalMaterial;
        }

        isFlashing = false;
    }

    // ==============================================
    // 🎨 에디터 기즈모
    // ==============================================
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = ballColor;
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}
