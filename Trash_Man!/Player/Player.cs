using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class Player : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float playerSpeed = 5f;
    [SerializeField] private float lerpSpeed = 10f;

    [SerializeField] private List<TrashData> availableTypes;
    [SerializeField] private float colorChangeDuration = 0.3f;

    [SerializeField] private int currentTrashStack = 0;
    [SerializeField] private int maxTrashCapacity = 100;

    [Header("Mobile Controls")]
    [SerializeField] private float minSwipeDistance = 50f;

    [Header("Player SFX")]
    public AudioClip can_sfx;
    public AudioClip trash_sfx;
    public AudioClip plastic_sfx;

    private int currentIndex = 0;
    private Vector2 touchStartPos;

    public TrashData CurrentData
    {
        get
        {
            if (availableTypes == null || availableTypes.Count == 0) return null;
            return availableTypes[currentIndex];
        }
    }

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Vector2 moveInput;

    private bool IsActing
    {
        get
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName("Burning");
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        UpdateColor(true);
    }

    void Update()
    {
        if (IsActing)
        {
            StopMovement();
            return;
        }

        if (CheckSwipeBurning())
        {
            return;
        }

        PlayerMovement();

        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeTypeDirect(2);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeTypeDirect(0);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeTypeDirect(1);
    }

    private void FixedUpdate()
    {
        if (moveInput.x != 0)
        {
            Vector2 targetSpeed = moveInput * playerSpeed;
            rb.velocity = Vector2.Lerp(rb.velocity, targetSpeed, Time.fixedDeltaTime * lerpSpeed);
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    private void PlayerMovement()
    {
        float x = 0f;

        // A. 키보드 입력
        if (Input.GetAxisRaw("Horizontal") != 0)
        {
            x = Input.GetAxisRaw("Horizontal");
        }
        // B. 모바일 터치 입력
        else if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // 버튼 위가 아니고, 스와이프 동작이 아닐 때만 이동 처리
            if (!IsPointerOverUI(touch) && touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved)
            {
                // 화면 절반 기준: 왼쪽 터치(-1), 오른쪽 터치(1)
                if (touch.position.x < Screen.width / 2) x = -1;
                else x = 1;
            }
        }
        // C. 에디터 마우스 테스트용
        else if (Input.GetMouseButton(0))
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                if (Input.mousePosition.x < Screen.width / 2) x = -1;
                else x = 1;
            }
        }

        moveInput = new Vector2(x, 0).normalized;

        if (x != 0)
        {
            spriteRenderer.flipX = x < 0;
        }

        if (animator != null)
        {
            animator.SetBool("isWalk", x != 0);
        }
    }

    private void StopMovement()
    {
        moveInput = Vector2.zero;
        rb.velocity = Vector2.zero;
        if (animator != null) animator.SetBool("isWalk", false);
    }

    private bool CheckSwipeBurning()
    {
        // 1. 키보드 테스트용 (Shift 키)
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            TriggerBurn();
            return true;
        }

        // 2. 모바일 터치 스와이프
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                if (CalculateSwipe(touch.position)) return true;
            }
        }

        // 3. PC 마우스 스와이프 (유니티 에디터 테스트용)
        // 마우스 클릭 시점 (터치 시작)
        if (Input.GetMouseButtonDown(0))
        {
            touchStartPos = Input.mousePosition;
        }
        // 마우스 뗄 때 (터치 종료)
        else if (Input.GetMouseButtonUp(0))
        {
            if (CalculateSwipe(Input.mousePosition)) return true;
        }

        return false;
    }

    // 스와이프 계산 로직
    private bool CalculateSwipe(Vector2 endPos)
    {
        float swipeDistanceY = endPos.y - touchStartPos.y;
        float swipeDistanceX = endPos.x - touchStartPos.x;

        // 아래로 일정 거리 이상 이동했는가?
        // 수평보다 수직 움직임이 더 큰가?
        if (swipeDistanceY < -minSwipeDistance && Mathf.Abs(swipeDistanceY) > Mathf.Abs(swipeDistanceX))
        {
            TriggerBurn();
            return true;
        }
        return false;
    }

    private void TriggerBurn()
    {
        StopMovement();
        if (animator != null) animator.SetTrigger("doBurn");

        if (currentTrashStack > 0)
        {
            GameManager.Instance.Incinerate(currentTrashStack);

            currentTrashStack = 0;
        }
    }

    public void AddTrashStack()
    {
        if (currentTrashStack < maxTrashCapacity)
        {
            currentTrashStack++;
        }
        else
        {
        }
    }

    // UI 터치 방지 헬퍼
    private bool IsPointerOverUI(Touch touch)
    {
        return EventSystem.current.IsPointerOverGameObject(touch.fingerId);
    }

    // 버튼용 함수
    public void ChangeTypeDirect(int targetIndex)
    {
        if (availableTypes == null || targetIndex < 0 || targetIndex >= availableTypes.Count) return;
        if (currentIndex == targetIndex) return;

        currentIndex = targetIndex;
        UpdateColor();
    }

    // 기존 순환형
    void ChangeType(int direction)
    {
        if (availableTypes == null || availableTypes.Count == 0) return;
        int count = availableTypes.Count;
        currentIndex = (currentIndex + direction + count) % count;
        UpdateColor();
    }

    void UpdateColor(bool immediate = false)
    {
        if (CurrentData == null) return;
        Color targetColor = CurrentData.playerColor;
        spriteRenderer.DOKill();

        if (immediate) spriteRenderer.color = targetColor;
        else spriteRenderer.DOColor(targetColor, colorChangeDuration);
    }
}
