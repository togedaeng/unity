using UnityEngine;
using UnityEngine.AI; // NavMeshAgent를 사용하기 위해 꼭 추가해야 합니다.

public class DogNavigation : MonoBehaviour
{
    [Header("랜덤 이동 설정")]
    public float wanderRadius = 6f; // 랜덤 이동 반경 (여유로운 움직임을 위해 작게 설정)
    public float minWaitTime = 2f; // 최소 대기 시간
    public float maxWaitTime = 5f; // 최대 대기 시간
    
    [Header("애니메이션 설정")]
    public float walkSpeedThreshold = 0.05f; // 걷기 판정을 위한 최소 속도 (더 민감하게)
    
    [Header("끼임 감지 및 탈출 설정")]
    public float stuckThreshold = 2f; // 끼임 판정 시간 (초)
    public float minVelocityThreshold = 0.1f; // 끼임 판정을 위한 최소 속도
    public float escapeRadius = 3f; // 탈출을 위한 새로운 목적지 탐색 반경
    public int maxEscapeAttempts = 5; // 최대 탈출 시도 횟수
    
    private NavMeshAgent agent; // NavMeshAgent 컴포넌트를 담을 변수
    private Animator animator; // Animator 컴포넌트를 담을 변수
    private float waitTimer; // 대기 타이머
    private bool isWaiting = false; // 대기 상태 확인
    private bool isWalking = false; // 현재 걷기 상태
    
    // 끼임 감지를 위한 변수들
    private float stuckTimer = 0f; // 끼임 시간 측정
    private bool isStuck = false; // 현재 끼임 상태
    private Vector3 lastPosition; // 이전 프레임의 위치
    private float positionCheckTimer = 0f; // 위치 체크 타이머

    // 클래스 상단 변수 추가
    private bool navigationPaused = false; // 네비게이션 일시 중지 상태
    private bool wasWaitingBeforePause = false; // 일시 중지 전 대기 상태 저장
    private Vector3 pausedPosition; // 일시 중지된 위치 저장
    private Quaternion pausedRotation; // 일시 중지된 회전 저장

    void Start()
    {
        // 스크립트가 적용된 오브젝트의 NavMeshAgent 컴포넌트를 가져옵니다.
        agent = GetComponent<NavMeshAgent>();
        
        // 스크립트가 적용된 오브젝트의 Animator 컴포넌트를 가져옵니다.
        animator = GetComponent<Animator>();
        
        // Animator 컴포넌트가 없다면 경고 메시지 출력
        if (animator == null)
        {
            Debug.LogWarning("Animator 컴포넌트를 찾을 수 없습니다!");
        }
        
        // 초기 위치 설정
        lastPosition = transform.position;
        
        // 시작하자마자 첫 번째 랜덤 목적지 설정
        SetRandomDestination();
    }

    void Update()
    {
        // 네비게이션이 일시 중지된 경우 위치와 회전 완전 고정
        if (navigationPaused)
        {
            // 위치와 회전 강제 고정
            transform.position = pausedPosition;
            transform.rotation = pausedRotation;
            
            // NavMeshAgent 상태 재확인
            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.velocity = Vector3.zero;
                agent.isStopped = true;
                agent.updatePosition = false;
                agent.updateRotation = false;
            }
            
            UpdateAnimation(); // 애니메이션은 계속 업데이트
            return;
        }
        
        // 끼임 감지 체크
        CheckIfStuck();
        
        // 애니메이션 상태 업데이트
        UpdateAnimation();
        
        // 대기 중이 아닐 때만 이동 상태 확인
        if (!isWaiting && !isStuck)
        {
            // 목적지에 도달했는지 확인 (remainingDistance가 거의 0이고 경로 계산이 완료되었을 때)
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                // 대기 모드로 전환 (랜덤 대기 시간 설정)
                isWaiting = true;
                waitTimer = Random.Range(minWaitTime, maxWaitTime);
            }
        }
        else if (isWaiting && !isStuck)
        {
            // 대기 시간 감소
            waitTimer -= Time.deltaTime;
            
            // 대기 시간이 끝나면 새로운 랜덤 목적지 설정
            if (waitTimer <= 0f)
            {
                SetRandomDestination();
                isWaiting = false;
            }
        }
    }

    void CheckIfStuck()
    {
        // 에이전트가 목표 지점을 향해 이동 중인지 확인
        if (agent.hasPath && !isWaiting)
        {
            // 현재 속도 확인
            float currentSpeed = agent.velocity.magnitude;
            
            // 위치 변화량 확인 (추가적인 끼임 감지 방법)
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            
            // 속도가 임계값보다 낮고 위치 변화도 거의 없다면 끼임으로 판단
            if (currentSpeed < minVelocityThreshold && distanceMoved < minVelocityThreshold)
            {
                stuckTimer += Time.deltaTime;
                
                // 일정 시간 이상 끼임 상태라면 탈출 로직 실행
                if (stuckTimer >= stuckThreshold && !isStuck)
                {
                    Debug.LogWarning("강아지가 끼었습니다! 탈출을 시도합니다.");
                    StartEscapeSequence();
                }
            }
            else
            {
                // 정상적으로 이동 중이면 타이머 초기화
                stuckTimer = 0f;
                if (isStuck)
                {
                    Debug.Log("강아지가 성공적으로 탈출했습니다!");
                    isStuck = false;
                }
            }
        }
        else
        {
            // 경로가 없거나 대기 중이면 타이머 초기화
            stuckTimer = 0f;
        }
        
        // 위치 업데이트 (0.1초마다)
        positionCheckTimer += Time.deltaTime;
        if (positionCheckTimer >= 0.1f)
        {
            lastPosition = transform.position;
            positionCheckTimer = 0f;
        }
    }

    void StartEscapeSequence()
    {
        isStuck = true;
        stuckTimer = 0f;
        
        // 현재 경로 초기화
        agent.ResetPath();
        
        // 탈출 시도
        bool escapeSuccessful = false;
        int attempts = 0;
        
        while (!escapeSuccessful && attempts < maxEscapeAttempts)
        {
            attempts++;
            
            // 더 넓은 범위에서 탈출 위치 찾기
            Vector3 escapeDirection = GetEscapeDirection();
            Vector3 escapePosition = transform.position + escapeDirection * escapeRadius;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(escapePosition, out hit, escapeRadius, NavMesh.AllAreas))
            {
                // 현재 위치에서 탈출 위치까지의 경로가 유효한지 확인
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.SetDestination(hit.position);
                    escapeSuccessful = true;
                    Debug.Log($"탈출 성공! 시도 횟수: {attempts}");
                }
            }
            
            if (!escapeSuccessful)
            {
                Debug.Log($"탈출 시도 {attempts} 실패, 재시도 중...");
            }
        }
        
        // 모든 시도가 실패했다면 기본 랜덤 목적지로 설정
        if (!escapeSuccessful)
        {
            Debug.LogWarning("모든 탈출 시도 실패! 기본 랜덤 이동으로 전환합니다.");
            SetRandomDestination();
        }
    }

    Vector3 GetEscapeDirection()
    {
        // 여러 방향을 시도해서 가장 적절한 탈출 방향 찾기
        Vector3[] directions = {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right,
            Vector3.forward + Vector3.right,
            Vector3.forward + Vector3.left,
            Vector3.back + Vector3.right,
            Vector3.back + Vector3.left
        };
        
        // 랜덤한 방향 선택
        Vector3 randomDirection = directions[Random.Range(0, directions.Length)];
        
        // 현재 Transform의 방향에 맞게 조정
        return transform.TransformDirection(randomDirection.normalized);
    }

    void UpdateAnimation()
    {
        if (animator == null) return;
        
        // NavMeshAgent의 현재 속도를 확인
        float currentSpeed = agent.velocity.magnitude;
        
        // 현재 걷고 있는지 판단 (속도가 임계값보다 큰 경우)
        bool shouldWalk = currentSpeed > walkSpeedThreshold;
        
        // Bool 파라미터로 Idle/Walk 상태 전환
        animator.SetBool("isWalking", shouldWalk);
        
        // 상태 변화 로깅 (디버그용 - 필요없으면 주석 처리)
        if (shouldWalk && !isWalking)
        {
            Debug.Log("강아지가 걷기 시작했습니다!");
            isWalking = true;
        }
        else if (!shouldWalk && isWalking)
        {
            Debug.Log("강아지가 멈췄습니다!");
            isWalking = false;
        }
        
        // === 트리거 방식을 사용하고 싶다면 아래 코드를 사용하세요 ===
        // 걷기 상태가 변경되었을 때만 트리거 실행
        // if (shouldWalk && !isWalking)
        // {
        //     // 걷기 시작
        //     animator.SetTrigger("Walk");
        //     isWalking = true;
        // }
        // else if (!shouldWalk && isWalking)
        // {
        //     // 걷기 중단
        //     animator.SetTrigger("Idle");
        //     isWalking = false;
        // }
    }

    void SetRandomDestination()
    {
        // 현재 위치에서 wanderRadius 반경 내의 랜덤 위치 생성
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;
        
        NavMeshHit hit;
        // NavMesh 위의 유효한 위치를 찾습니다
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            // 유효한 위치가 발견되면 해당 위치로 이동
            agent.SetDestination(hit.position);
        }
        else
        {
            // 유효한 위치를 찾지 못했다면 다시 시도
            SetRandomDestination();
        }
    }

    /// <summary>
    /// 상호작용 애니메이션 시작 시 네비게이션 완전 정지
    /// </summary>
    public void PauseNavigation()
    {
        if (navigationPaused) return;
        
        navigationPaused = true;
        wasWaitingBeforePause = isWaiting;
        
        pausedPosition = transform.position;
        pausedRotation = transform.rotation;
        
        if (agent != null)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
            agent.updateRotation = false;
            agent.updatePosition = false;
        }
        
        isWaiting = true;
        stuckTimer = 0f;
        isStuck = false;
    }

    /// <summary>
    /// 상호작용 애니메이션 완료 시 네비게이션 재개
    /// </summary>
    public void ResumeNavigation()
    {
        if (!navigationPaused) return;
        
        navigationPaused = false;
        
        if (agent != null)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;
            agent.velocity = Vector3.zero;
        }
        
        isWaiting = wasWaitingBeforePause;
        
        if (!isWaiting)
        {
            waitTimer = Random.Range(2f, 3f);
            isWaiting = true;
        }
        else
        {
            waitTimer = Random.Range(minWaitTime, maxWaitTime);
        }
    }

    /// <summary>
    /// 현재 네비게이션이 일시 중지 상태인지 확인
    /// </summary>
    public bool IsNavigationPaused()
    {
        return navigationPaused;
    }

    /// <summary>
    /// 지연된 네비게이션 재개
    /// </summary>
    private System.Collections.IEnumerator DelayedResumeNavigation()
    {
        // 애니메이션 완료 후 고정 시간 대기 (3초)
        yield return new WaitForSeconds(3f);
        
        DogNavigation dogNavigation = GetComponent<DogNavigation>();
        if (dogNavigation != null)
        {
            dogNavigation.ResumeNavigation();
            Debug.Log("🔓 네비게이션 지연 재개됨 (3초 대기 완료)");
        }
    }

    void LateUpdate()
    {
        // 네비게이션이 일시 중지된 경우 위치와 회전 재차 고정
        if (navigationPaused)
        {
            transform.position = pausedPosition;
            transform.rotation = pausedRotation;
        }
    }
}