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
    
    private NavMeshAgent agent; // NavMeshAgent 컴포넌트를 담을 변수
    private Animator animator; // Animator 컴포넌트를 담을 변수
    private float waitTimer; // 대기 타이머
    private bool isWaiting = false; // 대기 상태 확인
    private bool isWalking = false; // 현재 걷기 상태

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
        
        // 시작하자마자 첫 번째 랜덤 목적지 설정
        SetRandomDestination();
    }

    void Update()
    {
        // 애니메이션 상태 업데이트
        UpdateAnimation();
        
        // 대기 중이 아닐 때만 이동 상태 확인
        if (!isWaiting)
        {
            // 목적지에 도달했는지 확인 (remainingDistance가 거의 0이고 경로 계산이 완료되었을 때)
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                // 대기 모드로 전환 (랜덤 대기 시간 설정)
                isWaiting = true;
                waitTimer = Random.Range(minWaitTime, maxWaitTime);
            }
        }
        else
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
}