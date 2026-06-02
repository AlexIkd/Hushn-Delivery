using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerRailRide_SonicStyle_Spline : MonoBehaviour
{
    [Header("Configuracoes de Velocidade")]
    [SerializeField] private float baseSpeed = 15f;
    [SerializeField] private float maxSpeed = 35f;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float boostSpeed = 50f;
    [SerializeField] private float boostAcceleration = 20f;
    
    [Header("Configuracoes de Entrada")]
    [SerializeField] private float autoEnterRadius = 3f;
    [SerializeField] private float autoEnterAngle = 60f;
    [SerializeField] private float minSpeedToEnter = 5f;
    [SerializeField] private float reEnterCooldown = 0.5f; // Tempo minimo apos sair do grind para poder entrar novamente
    
    [Header("Configuracoes de Salto")]
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float lateralJumpForce = 5f;
    [SerializeField] private float lateralJumpDistance = 5f;
    [SerializeField] private float exitSpeedMultiplier = 0.5f;
    [SerializeField] private bool autoJumpAtEnd = true; // Pula automaticamente ao chegar no fim do rail
    [SerializeField] private float autoJumpForce = 8f; // Forca do pulo automatico ao fim do rail
    [SerializeField] private float earlyJumpDistance = 2f; // Distancia antes do fim para pular (em metros)
    
    [Header("Configuracoes de Rotacao")]
    [SerializeField] private float rotationLerpSpeed = 10f; // Velocidade de rotacao proporcional ao percurso
    
    [Header("Configuracoes de Colisao")]
    [SerializeField] private float railHeightOffset = 0.5f; // Offset para evitar colisao com o rail
    [SerializeField] private LayerMask railLayer; // Layer do rail para ignorar colisao
    
    [Header("Configuracoes de Troca de Rail")]
    [SerializeField] private float railSwitchCooldown = 0.3f; // Tempo minimo entre trocas de rail
    [SerializeField] private float railSwitchSpeed = 12f; // Velocidade da transicao entre rails
    [SerializeField] private float railSwitchDuration = 0.2f; // Duracao da transicao
    [SerializeField] private float railSwitchMaxDistance = 3.0f; // Distancia maxima para permitir a troca de rail
    
    [Header("Configuracoes de Boost")]
    [SerializeField] private bool infiniteBoost = false;
    [SerializeField] private float boostDuration = 3f;
    [SerializeField] private float boostCooldown = 1f;
    
    [Header("Animacao")]
    [SerializeField] private string grindAnimationName = "Grind";
    [SerializeField] private string railSwitchLeftAnimationName = "RailSwitchLeft"; // Animacao de troca para esquerda
    [SerializeField] private string railSwitchRightAnimationName = "RailSwitchRight"; // Animacao de troca para direita
    
    [Header("Anime Speed Lines")]
    [SerializeField] private PlayerAnimeSpeedLines speedLines; // Referencia ao componente de linhas de velocidade
    [SerializeField] private bool enableSpeedLinesOnSwitch = true; // Habilita linhas de velocidade durante troca de rail
    
    [Header("Particulas de Grind")]
    [SerializeField] private ParticleSystem grindParticles; // Sistema de particulas como child do personagem (grind normal)
    [SerializeField] private bool enableGrindParticles = true; // Habilita particulas durante grind normal
    [SerializeField] private ParticleSystem switchGrindParticles; // Sistema de particulas como child do personagem (troca de rail)
    [SerializeField] private bool enableSwitchGrindParticles = true; // Habilita particulas durante troca de rail
    [SerializeField] private ParticleSystem boostParticles; // Sistema de particulas para boost
    [SerializeField] private bool enableBoostParticles = true; // Habilita particulas durante boost
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    // Estado
    public bool isGrinding = false;
    private bool isBoosting = false;
    private float boostTimer = 0f;
    private float boostCooldownTimer = 0f;
    private bool hasTriggeredEarlyJump = false; // Flag para evitar multiplos pulos
        private float reEnterCooldownTimer = 0f; // Timer para cooldown de reentrada no grind
    private float collisionRestoreTimer = 0f; // Timer para restaurar a colisao apos sair do rail
    private Rail_SonicStyle_Spline lastRail; // Armazena o ultimo rail para restaurar colisao
    
    // Rail atual (usando spline)

    private Rail_SonicStyle_Spline currentRail;
    private float currentT = 0f;
    private bool movingForward = true;
    private float currentSpeed;
    private float targetSpeed;
    
    // Componentes
    private CharacterController controller;
    private StyleRankSystem styleRankSystem;
    private PlayerMovement_FrontiersStyle movement;
    private Transform modelTransform;
    private Animator animator;
    
    // Tempo no rail
    private float grindTime = 0f;
    
    // Salva rotacao antes do grind
    private bool wasRotationLocked = false;
    
    // Controle de colisao
    private Collider[] railColliders;
    
    // Controle de troca de rail
    private bool isSwitchingRail = false;
    private float railSwitchTimer = 0f;
    private float railSwitchCooldownTimer = 0f;
    private Rail_SonicStyle_Spline targetRail;
    private Vector3 switchStartPosition;
    private Vector3 switchTargetPosition;
    private float switchStartT;
    private float switchTargetT; // T no rail de destino durante a troca
    private float switchSpeed; // Velocidade salva no momento da troca
    


    void Start()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement_FrontiersStyle>();
        animator = GetComponent<Animator>();

        if (controller == null)
        {
            Debug.LogError("CharacterController nao encontrado!");
        }

        if (movement == null)
        {
            Debug.LogWarning("PlayerMovement_FrontiersStyle nao encontrado.");
        }

        // Tenta encontrar o StyleRankSystem no mesmo GameObject
        styleRankSystem = GetComponent<StyleRankSystem>();
        if (styleRankSystem == null)
        {
            Debug.LogWarning("StyleRankSystem nao encontrado. A pontuacao de estilo nao sera registrada.");
        }

        // Encontra o modelo do personagem
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length > 0)
        {
            modelTransform = renderers[0].transform;
        }
        else
        {
            modelTransform = transform;
        }
    }

    void Update()
    {
        if (boostCooldownTimer > 0)
        {
            boostCooldownTimer -= Time.deltaTime;
        }

        if (railSwitchCooldownTimer > 0)
        {
            railSwitchCooldownTimer -= Time.deltaTime;
        }

        if (reEnterCooldownTimer > 0)
        {
            reEnterCooldownTimer -= Time.deltaTime;
        }

        // ✅ NOVO: Gerencia a restauracao da colisao
        if (collisionRestoreTimer > 0)
        {
            collisionRestoreTimer -= Time.deltaTime;
            if (collisionRestoreTimer <= 0 && lastRail != null)
            {
                IgnoreRailCollision(lastRail, false);
                lastRail = null;
            }
        }

        if (isGrinding)
        {
            grindTime += Time.deltaTime;
            
            if (isSwitchingRail)
            {
                HandleRailSwitch();
            }
            else
            {
                HandleGrindingInput();
                FollowRail();
            }
            
            HandleBoost();
        }
        else
        {
            TryAutoEnterRail();
        }
    }

    private void TryAutoEnterRail()
    {
        // Verifica se ainda esta em cooldown de reentrada
        if (reEnterCooldownTimer > 0)
            return;

        if (movement == null || movement.CurrentSpeed < minSpeedToEnter)
            return;

        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, autoEnterRadius);
        
        foreach (Collider col in nearbyColliders)
        {
            Rail_SonicStyle_Spline rail = col.GetComponent<Rail_SonicStyle_Spline>();
            if (rail != null)
            {
                float distance = rail.GetDistanceToSpline(transform.position);
                if (distance > autoEnterRadius) continue;

                float t = rail.GetClosestT(transform.position);
                Vector3 railDirection = rail.GetTangentAtT(t);
                Vector3 playerDirection = transform.forward;
                
                float angle = Vector3.Angle(playerDirection, railDirection);
                float reverseAngle = Vector3.Angle(playerDirection, -railDirection);
                
                // Verifica se esta alinhado em qualquer direcao
                if (angle <= autoEnterAngle || reverseAngle <= autoEnterAngle)
                {
                    EnterRail(rail);
                    break;
                }
            }
        }
    }

    private void HandleGrindingInput()
    {
        // Troca de rail com A/D (relativo a direcao do jogador no rail)
        if (railSwitchCooldownTimer <= 0)
        {
            // Determina qual lado e esquerda/direita baseado na direcao do movimento
            bool inputLeft = Input.GetKeyDown(KeyCode.A);
            bool inputRight = Input.GetKeyDown(KeyCode.D);
            
            if (inputLeft || inputRight)
            {
                // Calcula a direcao relativa ao movimento do jogador
                bool switchToLeftRail = DetermineRelativeRailDirection(inputLeft);
                
            if (currentRail.HasAdjacentRail(switchToLeftRail))
            {
                // 1. Obtem o rail adjacente
                Rail_SonicStyle_Spline targetRailCandidate = currentRail.GetAdjacentRail(switchToLeftRail);

                if (targetRailCandidate != null)
                {
                    // 2. Encontra o ponto mais proximo no rail adjacente
                    float closestT = targetRailCandidate.GetClosestT(transform.position);
                    Vector3 closestPoint = targetRailCandidate.GetPositionAtT(closestT);

                    // 3. Calcula a distancia
                    float distanceToTargetRail = Vector3.Distance(transform.position, closestPoint);

                    // 4. Verifica se a distancia esta dentro do limite
                    if (distanceToTargetRail <= railSwitchMaxDistance)
                    {
                        StartRailSwitch(switchToLeftRail);
                        return;
                    }
                    else if (showDebugInfo)
                    {
                        Debug.Log("Troca de Rail Bloqueada: Distancia (" + distanceToTargetRail.ToString("F2") + "m) excede o maximo permitido (" + railSwitchMaxDistance.ToString("F2") + "m).");
                    }
                }
            }
            }
        }
        
        // Pulo para sair do rail agora e gerenciado pelo PlayerMovement_FrontiersStyle
        // para garantir a sincronia do pulo padrao.

        // Boost
        if (Input.GetKey(KeyCode.LeftShift) && !isBoosting && boostCooldownTimer <= 0)
        {
            StartBoost();
        }
    }

    /// <summary>
    /// Determina a direcao do rail (esquerda/direita) relativa ao movimento do jogador
    /// </summary>
    private bool DetermineRelativeRailDirection(bool inputLeft)
    {
        // Obtem a direcao atual do movimento no rail
        Vector3 railDirection = currentRail.GetTangentAtT(currentT);
        if (!movingForward)
        {
            railDirection = -railDirection;
        }
        
        // Calcula o vetor "direita" relativo a direcao do rail
        Vector3 railRight = Vector3.Cross(Vector3.up, railDirection).normalized;
        
        // Se o jogador esta se movendo para tras no rail, inverte a logica
        // Isso garante que A sempre va para a esquerda visual e D para a direita visual
        if (movingForward)
        {
            // Movendo para frente: A = esquerda (leftRail), D = direita (rightRail)
            return inputLeft;
        }
        else
        {
            // Movendo para tras: A = direita (rightRail), D = esquerda (leftRail)
            return !inputLeft;
        }
    }

    private void StartRailSwitch(bool switchLeft)
    {
        // Adiciona pontos de estilo ao trocar de rail
        styleRankSystem?.OnSwitchRailUsed();

        Rail_SonicStyle_Spline adjacentRail = currentRail.GetAdjacentRail(switchLeft);
        
        if (adjacentRail == null)
        {
            Debug.LogWarning("Rail adjacente nao encontrado!");
            return;
        }

        isSwitchingRail = true;
        railSwitchTimer = 0f;
        targetRail = adjacentRail;
        
        // Salva a velocidade atual para manter durante a troca
        switchSpeed = currentSpeed;
        
        // Salva posicao inicial e T inicial de ambos os rails
        switchStartPosition = transform.position;
        switchStartT = currentT;
        
        // Calcula T inicial no rail adjacente (ponto mais proximo)
        switchTargetT = adjacentRail.GetClosestT(transform.position);
        switchTargetPosition = adjacentRail.GetPositionAtT(switchTargetT);
        switchTargetPosition += Vector3.up * railHeightOffset;
        
        // Ignora colisao com o rail de destino
        IgnoreRailCollision(adjacentRail, true);
        
        // Ativa animacao de troca de rail baseada na direcao do input
        if (animator != null)
        {
            // A variavel 'switchLeft' indica se o rail adjacente e o 'leftRail' do currentRail.
            // A animacao deve ser baseada no input do jogador (A ou D), que e o que determina
            // a direcao visual da animacao (esquerda ou direita).
            // Se 'switchLeft' for true, o jogador esta indo para o 'leftRail' do currentRail.
            // Se 'movingForward' for true, o input 'A' levou a 'switchLeft' = true.
            // Se 'movingForward' for false, o input 'D' levou a 'switchLeft' = true.
            
            // O input do jogador (A ou D) e o que define a direcao visual da animacao.
            // Na funcao HandleGrindingInput, 'inputLeft' (tecla A) e usado para chamar
            // DetermineRelativeRailDirection, que retorna 'switchLeft'.
            // Precisamos saber se o input original foi 'A' (esquerda visual) ou 'D' (direita visual).
            
            // Revertendo a logica de DetermineRelativeRailDirection para obter o input visual:
            bool visualLeftInput;
            if (movingForward)
            {
                // Movendo para frente: switchLeft == inputLeft (A)
                visualLeftInput = switchLeft;
            }
            else
            {
                // Movendo para tras: switchLeft == !inputLeft (D)
                // Entao, inputLeft (A) == !switchLeft
                visualLeftInput = !switchLeft;
            }
            
            if (visualLeftInput && !string.IsNullOrEmpty(railSwitchLeftAnimationName))
            {
                animator.SetTrigger(railSwitchLeftAnimationName);
            }
            else if (!visualLeftInput && !string.IsNullOrEmpty(railSwitchRightAnimationName))
            {
                animator.SetTrigger(railSwitchRightAnimationName);
            }
        }
        
        // Para particulas de grind durante a troca
        StopGrindParticles();
        
        // Inicia particulas de switch grind
        if (enableSwitchGrindParticles && switchGrindParticles != null)
        {
            StartSwitchGrindParticles();
        }
        
        // Ativa linhas de velocidade durante a troca de rail
        if (enableSpeedLinesOnSwitch && speedLines != null)
        {
            // Calcula a direção do movimento no rail atual
            Vector3 travelDirection = currentRail.GetTangentAtT(currentT);
            if (!movingForward) travelDirection = -travelDirection;

            // Chama EnableEffect com a direção
            speedLines.EnableEffect(travelDirection);
        }
        
        Debug.Log("Iniciando troca para rail " + (switchLeft ? "esquerdo" : "direito") + ": " + adjacentRail.gameObject.name);
    }

    private void HandleRailSwitch()
    {
        railSwitchTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(railSwitchTimer / railSwitchDuration);
        
        // CONTINUA AVANCANDO nos rails durante a transicao usando a velocidade salva
        float distance = switchSpeed * Time.deltaTime;
        
        // Avanca no rail atual (origem)
        float currentRailT = currentRail.AdvanceByDistance(switchStartT, distance * (1f - progress), movingForward);
        Vector3 positionOnCurrentRail = currentRail.GetPositionAtT(currentRailT);
        positionOnCurrentRail += Vector3.up * railHeightOffset;
        
        // Avanca no rail de destino
        switchTargetT = targetRail.AdvanceByDistance(switchTargetT, distance, movingForward);
        Vector3 positionOnTargetRail = targetRail.GetPositionAtT(switchTargetT);
        positionOnTargetRail += Vector3.up * railHeightOffset;
        
        // Interpola entre as posicoes atualizadas dos dois rails
        Vector3 finalPosition = Vector3.Lerp(positionOnCurrentRail, positionOnTargetRail, progress);
        
        // Move o personagem
        if (controller != null && controller.enabled)
        {
            controller.enabled = false;
            transform.position = finalPosition;
            controller.enabled = true;
        }
        else
        {
            transform.position = finalPosition;
        }
        
        // Rotaciona suavemente para a direcao do novo rail
        Vector3 direction = targetRail.GetTangentAtT(switchTargetT);
        if (!movingForward) direction = -direction;
        
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationLerpSpeed * 2f);
        }
        
        // Completa a transicao
        if (progress >= 1f)
        {
            CompleteRailSwitch();
        }
    }

    private void CompleteRailSwitch()
    {
        // Restaura colisao com o rail antigo
        if (currentRail != null)
        {
            IgnoreRailCollision(currentRail, false);
        }
        
        // Muda para o novo rail usando o T ja calculado durante a transicao
        currentRail = targetRail;
        currentT = switchTargetT; // Usa o T que foi sendo atualizado durante a troca
        
        // Mantem a direcao do movimento
        Vector3 currentDirection = transform.forward;
        Vector3 railTangent = currentRail.GetTangentAtT(currentT);
        float dotProduct = Vector3.Dot(currentDirection.normalized, railTangent.normalized);
        movingForward = dotProduct >= 0;
        
        // Finaliza transicao
        isSwitchingRail = false;
        railSwitchCooldownTimer = railSwitchCooldown;
        targetRail = null;
        
        // Para particulas de switch grind
        StopSwitchGrindParticles();
        
        // Reinicia particulas de grind apos a troca
        if (enableGrindParticles && grindParticles != null)
        {
            StartGrindParticles();
        }
        
        // Reseta flag de pulo antecipado ao trocar de rail
        hasTriggeredEarlyJump = false;
        
        // Nota: As linhas de velocidade se desativam automaticamente após a duração configurada
        
        Debug.Log("Troca de rail completa! Novo rail: " + currentRail.gameObject.name);
    }

    private void FollowRail()
    {
        if (currentRail == null) return;

        // Atualiza velocidade
        if (isBoosting)
        {
            targetSpeed = boostSpeed;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, boostAcceleration * Time.deltaTime);
        }
        else
        {
            targetSpeed = Mathf.Min(currentRail.RecommendedSpeed, maxSpeed);
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        }

        // Avanca no spline baseado na velocidade e direcao
        float distance = currentSpeed * Time.deltaTime;
        currentT = currentRail.AdvanceByDistance(currentT, distance, movingForward);

        // NOVA LOGICA: Verifica distancia ate o final do rail
        if (autoJumpAtEnd && !hasTriggeredEarlyJump)
        {
            float splineLength = currentRail.GetSplineLength();
            float currentDistance = currentT * splineLength;
            float distanceToEnd = movingForward ? (splineLength - currentDistance) : currentDistance;
            
            // Se estiver proximo do fim, executa o pulo antecipado
            if (distanceToEnd <= earlyJumpDistance)
            {
                hasTriggeredEarlyJump = true;
                ExitRailWithEarlyJump();
                return;
            }
        }

        // Verifica se chegou ao fim (fallback caso o pulo antecipado nao funcione)
        if (currentRail.IsAtEnd(currentT, movingForward))
        {
            if (!hasTriggeredEarlyJump)
            {
                ExitRailAtEnd();
            }
            return;
        }

        // Obtem posicao e direcao no spline
        Vector3 targetPosition = currentRail.GetPositionAtT(currentT);
        
        // Aplica offset de altura para evitar colisao com o rail
        targetPosition += Vector3.up * railHeightOffset;
        
        Vector3 direction = currentRail.GetTangentAtT(currentT);
        
        // Inverte a direcao se estiver movendo para tras
        if (!movingForward)
        {
            direction = -direction;
        }

        // Move o personagem diretamente (sem usar CharacterController.Move para evitar colisoes)
        if (controller != null && controller.enabled)
        {
            // Desabilita temporariamente o CharacterController para mover diretamente
            controller.enabled = false;
            transform.position = targetPosition;
            controller.enabled = true;
        }
        else
        {
            transform.position = targetPosition;
        }

        // Rotaciona o personagem proporcionalmente ao percurso do rail
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationLerpSpeed);
        }

        // Logica de Camera Cinematografica (agora no Rail)
        currentRail?.CheckCinematicCamera(currentT, movingForward);
    }

    private void HandleBoost()
    {
        if (isBoosting)
        {
            boostTimer -= Time.deltaTime;
            
            if (boostTimer <= 0 && !infiniteBoost)
            {
                StopBoost();
            }
            
            if (!Input.GetKey(KeyCode.LeftShift))
            {
                StopBoost();
            }
        }
    }

    private void StartBoost()
    {
        // Adiciona pontos de estilo ao usar boost
        styleRankSystem?.OnBoostUsed();

        isBoosting = true;
        boostTimer = boostDuration;
        
        // Inicia particulas de boost
        if (enableBoostParticles && boostParticles != null)
        {
            StartBoostParticles();
        }
        
        Debug.Log("BOOST ativado!");
    }

    private void StopBoost()
    {
        isBoosting = false;
        boostCooldownTimer = boostCooldown;
        
        // Para particulas de boost
        StopBoostParticles();

        // Desativa o efeito de FOV/SpeedLines
        if (speedLines != null)
        {
            speedLines.DisableEffect();
        }
        
        Debug.Log("BOOST desativado");
    }

    public void EnterRail(Rail_SonicStyle_Spline rail)
    {
        if (rail == null)
        {
            Debug.LogError("Tentativa de entrar em um rail null!");
            return;
        }

        // Adiciona pontos de estilo ao iniciar o grind
        styleRankSystem?.OnGrindRailStart();

        currentRail = rail;
        
        // Encontra o ponto mais proximo no spline
        currentT = rail.GetClosestT(transform.position);
        
        // Determina direcao baseado na velocidade do jogador
        if (movement != null)
        {
            Vector3 velocity = transform.forward * movement.CurrentSpeed;
            
            // Obtem a tangente do rail no ponto mais proximo
            Vector3 railTangent = rail.GetTangentAtT(currentT);
            
            // Calcula o produto escalar para determinar a direcao
            float dotProduct = Vector3.Dot(velocity.normalized, railTangent.normalized);
            
            // Se o dot product for positivo, move para frente; se negativo, move para tras
            movingForward = dotProduct >= 0;
            
            Debug.Log("Direcao determinada: " + (movingForward ? "Frente" : "Tras") + " (Dot: " + dotProduct.ToString("F2") + ")");
        }
        else
        {
            movingForward = true;
        }

        isGrinding = true;
        grindTime = 0f;
        isSwitchingRail = false;
        railSwitchCooldownTimer = 0f;
        hasTriggeredEarlyJump = false; // Reseta flag ao entrar no rail

        // Define velocidade inicial
        if (movement != null)
        {
            currentSpeed = Mathf.Max(movement.CurrentSpeed, baseSpeed);
            movement.ForceExitWallRun();
            
            // Reseta cargas de pulo duplo e air dash
            movement.ResetAirCharges();
            
            // Salva estado de rotacao
            wasRotationLocked = movement.IsRotationLocked;
            movement.LockRotation(false);

            // Desabilita a gravidade acumulada ao entrar no rail
            movement.ResetVerticalVelocity();
        }
        else
        {
            currentSpeed = baseSpeed;
        }

        // Ignora colisao com o rail
        IgnoreRailCollision(rail, true);

        // Define a rotacao inicial do transform para a direcao do rail
        Vector3 direction = rail.GetTangentAtT(currentT);
        if (!movingForward) direction = -direction;
        
        if (direction.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // Posiciona no spline com offset
        Vector3 railPosition = rail.GetPositionAtT(currentT);
        railPosition += Vector3.up * railHeightOffset;
        transform.position = railPosition;
        
        // Inicia animacao de grind
        if (animator != null && !string.IsNullOrEmpty(grindAnimationName))
        {
            animator.SetBool(grindAnimationName, true);
        }
        
        // Inicia particulas de grind
        if (enableGrindParticles && grindParticles != null)
        {
            StartGrindParticles();
        }

        Debug.Log("Entrou no rail '" + rail.gameObject.name + "' (Auto) - T=" + currentT.ToString("F2"));
    }

    private void IgnoreRailCollision(Rail_SonicStyle_Spline rail, bool ignore)
    {
        if (rail == null) return;

        // Obtem todos os colliders do rail
        Collider[] railCols = rail.GetComponentsInChildren<Collider>();
        Collider playerCollider = GetComponent<Collider>();
        
        // Se tiver CharacterController, usa o collider dele
        if (controller != null)
        {
            foreach (Collider railCol in railCols)
            {
                // Ignora colisao entre o player e o rail
                Physics.IgnoreCollision(railCol, controller, ignore);
            }
        }
        else if (playerCollider != null)
        {
            foreach (Collider railCol in railCols)
            {
                Physics.IgnoreCollision(railCol, playerCollider, ignore);
            }
        }

        if (ignore)
        {
            railColliders = railCols;
            Debug.Log("Colisao com rail IGNORADA (" + railCols.Length + " colliders)");
        }
        else
        {
            Debug.Log("Colisao com rail RESTAURADA");
        }
    }



    /// <summary>
    /// NOVA FUNCAO: Sai do rail com pulo antecipado antes do final
    /// </summary>
    private void ExitRailWithEarlyJump()
    {
        if (!isGrinding) return;
        currentRail?.DeactivateCinematicCamera();

        isGrinding = false;
        isBoosting = false;
        isSwitchingRail = false;

        float finalGrindTime = grindTime;
        
        // ✅ NOVO: Cooldown de colisao
        if (currentRail != null)
        {
            lastRail = currentRail;
            collisionRestoreTimer = 0.2f;
        }

        // Restaura colisao com o rail de destino se estava trocando
        if (targetRail != null)
        {
            IgnoreRailCollision(targetRail, false);
            targetRail = null;
        }
        
        // Para animacao de grind
        if (animator != null && !string.IsNullOrEmpty(grindAnimationName))
        {
            animator.SetBool(grindAnimationName, false);
        }
        
        // Para particulas de grind
        StopGrindParticles();
        
        // CORRECAO: Para particulas de boost ao sair do rail
        StopBoostParticles();
        
        // Aplica pulo antecipado
        Vector3 jumpVelocity = Vector3.up * autoJumpForce;
        Vector3 forwardVelocity = transform.forward * (currentSpeed * exitSpeedMultiplier);
        
        if (movement != null)
        {
            movement.AddExternalVelocity(forwardVelocity + jumpVelocity);
        }
        
        Debug.Log("Pulo antecipado ativado! (Distancia: " + earlyJumpDistance.ToString("F1") + "m antes do fim, Tempo: " + finalGrindTime.ToString("F1") + "s, Velocidade: " + currentSpeed.ToString("F1") + ")");

        currentRail = null;
        grindTime = 0f;
        
        // Ativa cooldown de reentrada
        reEnterCooldownTimer = reEnterCooldown;
        
        // Restaura estado de rotacao
        if (movement != null)
        {
            movement.LockRotation(wasRotationLocked);
        }
    }

        public void ExitRailForced()
    {
        if (!isGrinding) return;
        currentRail?.DeactivateCinematicCamera();

        isGrinding = false;
        isBoosting = false;
        isSwitchingRail = false;
        
        // ✅ NOVO: Nao restaura a colisao imediatamente para evitar "agarre"
        if (currentRail != null)
        {
            lastRail = currentRail;
            collisionRestoreTimer = 0.2f; // Mantem a colisao ignorada por 0.2 segundos
        }
        
        // Restaura colisao com o rail de destino se estava trocando
        if (targetRail != null)
        {
            IgnoreRailCollision(targetRail, false);
            targetRail = null;
        }
        
        // Para animacao de grind
        if (animator != null && !string.IsNullOrEmpty(grindAnimationName))
        {
            animator.SetBool(grindAnimationName, false);
        }
        
        // Para particulas
        StopGrindParticles();
        StopBoostParticles();
        StopSwitchGrindParticles();

        currentRail = null;
        grindTime = 0f;
        hasTriggeredEarlyJump = false;
        reEnterCooldownTimer = reEnterCooldown;
    }

    private void ExitRail(bool jumpOff)
    {
        if (!isGrinding) return;

        currentRail?.DeactivateCinematicCamera();

        isGrinding = false;
        isBoosting = false;
        isSwitchingRail = false;

        float finalGrindTime = grindTime;
        
        // ✅ NOVO: Cooldown de colisao
        if (currentRail != null)
        {
            lastRail = currentRail;
            collisionRestoreTimer = 0.2f;
        }

        // Restaura colisao com o rail de destino se estava trocando
        if (targetRail != null)
        {
            IgnoreRailCollision(targetRail, false);
            targetRail = null;
        }
        
        // Para animacao de grind
        if (animator != null && !string.IsNullOrEmpty(grindAnimationName))
        {
            animator.SetBool(grindAnimationName, false);
        }
        
        // Para particulas de grind
        StopGrindParticles();
        
        // CORRECAO: Para particulas de boost ao sair do rail
        StopBoostParticles();
        
        if (jumpOff)
        {
            Vector3 jumpVelocity = Vector3.up * jumpForce;
            Vector3 forwardVelocity = transform.forward * (currentSpeed * exitSpeedMultiplier);
            
            if (movement != null)
            {
                movement.AddExternalVelocity(forwardVelocity + jumpVelocity);
            }
            
            Debug.Log("Saiu do rail com pulo (Tempo: " + finalGrindTime.ToString("F1") + "s, Velocidade: " + currentSpeed.ToString("F1") + ")");
        }
        else
        {
            if (movement != null)
            {
                Vector3 exitVelocity = transform.forward * (currentSpeed * exitSpeedMultiplier);
                movement.AddExternalVelocity(exitVelocity);
            }
            
            Debug.Log("Saiu do rail (Tempo: " + finalGrindTime.ToString("F1") + "s)");
        }

        currentRail = null;
        grindTime = 0f;
        hasTriggeredEarlyJump = false;
        
        // Ativa cooldown de reentrada
        reEnterCooldownTimer = reEnterCooldown;
        
        // Restaura estado de rotacao
        if (movement != null)
        {
            movement.LockRotation(wasRotationLocked);
        }
    }

    private void ExitRailAtEnd()
    {
        if (!isGrinding) return;
        currentRail?.DeactivateCinematicCamera();

        isGrinding = false;
        isBoosting = false;
        isSwitchingRail = false;

        float finalGrindTime = grindTime;
        
        // ✅ NOVO: Cooldown de colisao
        if (currentRail != null)
        {
            lastRail = currentRail;
            collisionRestoreTimer = 0.2f;
        }

        // Restaura colisao com o rail de destino se estava trocando
        if (targetRail != null)
        {
            IgnoreRailCollision(targetRail, false);
            targetRail = null;
        }
        
        // Para animacao de grind
        if (animator != null && !string.IsNullOrEmpty(grindAnimationName))
        {
            animator.SetBool(grindAnimationName, false);
        }
        
        // Para particulas de grind
        StopGrindParticles();
        
        // CORRECAO: Para particulas de boost ao sair do rail
        StopBoostParticles();
        
        // Pulo automatico ao fim do rail (se habilitado)
        if (autoJumpAtEnd)
        {
            Vector3 jumpVelocity = Vector3.up * autoJumpForce;
            Vector3 forwardVelocity = transform.forward * (currentSpeed * exitSpeedMultiplier);
            
            if (movement != null)
            {
                movement.AddExternalVelocity(forwardVelocity + jumpVelocity);
            }
            
            Debug.Log("Fim do rail - Pulo automatico! (Tempo: " + finalGrindTime.ToString("F1") + "s, Velocidade: " + currentSpeed.ToString("F1") + ")");
        }
        else
        {
            // Sem pulo automatico, apenas aplica velocidade horizontal
            if (movement != null)
            {
                Vector3 exitVelocity = transform.forward * (currentSpeed * exitSpeedMultiplier);
                movement.AddExternalVelocity(exitVelocity);
            }
            
            Debug.Log("Fim do rail (Tempo: " + finalGrindTime.ToString("F1") + "s)");
        }

        currentRail = null;
        grindTime = 0f;
        hasTriggeredEarlyJump = false;
        
        // Ativa cooldown de reentrada
        reEnterCooldownTimer = reEnterCooldown;
        
        // Restaura estado de rotacao
        if (movement != null)
        {
            movement.LockRotation(wasRotationLocked);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isGrinding) return;

        Rail_SonicStyle_Spline rail = other.GetComponent<Rail_SonicStyle_Spline>();
        if (rail != null && movement != null && movement.CurrentSpeed >= minSpeedToEnter)
        {
            EnterRail(rail);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(transform.position, autoEnterRadius);
        
        if (isGrinding && currentRail != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 currentPos = currentRail.GetPositionAtT(currentT);
            Gizmos.DrawWireSphere(currentPos, 0.4f);
            
            // Mostra a posicao com offset
            Vector3 offsetPos = currentPos + Vector3.up * railHeightOffset;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(offsetPos, 0.3f);
            Gizmos.DrawLine(currentPos, offsetPos);
            
            Gizmos.color = Color.yellow;
            Vector3 direction = currentRail.GetTangentAtT(currentT);
            if (!movingForward) direction = -direction;
            Gizmos.DrawRay(transform.position, direction * 2f);
            
            // Visualiza o ponto de pulo antecipado
            if (autoJumpAtEnd)
            {
                float splineLength = currentRail.GetSplineLength();
                float earlyJumpT = movingForward ? 
                    Mathf.Clamp01((splineLength - earlyJumpDistance) / splineLength) : 
                    Mathf.Clamp01(earlyJumpDistance / splineLength);
                
                Vector3 earlyJumpPos = currentRail.GetPositionAtT(earlyJumpT);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(earlyJumpPos, 0.5f);
            }
            
            if (currentRail.HasAdjacentRail(true))
            {
                Gizmos.color = Color.green;
                Rail_SonicStyle_Spline leftRail = currentRail.GetAdjacentRail(true);
                float leftT = leftRail.GetClosestT(transform.position);
                Vector3 leftPos = leftRail.GetPositionAtT(leftT);
                Gizmos.DrawLine(transform.position, leftPos);
                Gizmos.DrawWireSphere(leftPos, 0.25f);
            }
            
            if (currentRail.HasAdjacentRail(false))
            {
                Gizmos.color = Color.magenta;
                Rail_SonicStyle_Spline rightRail = currentRail.GetAdjacentRail(false);
                float rightT = rightRail.GetClosestT(transform.position);
                Vector3 rightPos = rightRail.GetPositionAtT(rightT);
                Gizmos.DrawLine(transform.position, rightPos);
                Gizmos.DrawWireSphere(rightPos, 0.25f);
            }
            
            // Mostra transicao de rail
            if (isSwitchingRail && targetRail != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(switchStartPosition, switchTargetPosition);
                Gizmos.DrawWireSphere(switchTargetPosition, 0.3f);
            }
        }
    }

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        int yOffset = 210;
        
        GUI.Label(new Rect(10, yOffset, 400, 20), "=== RAIL GRIND (Sonic Spline) ===");
        GUI.Label(new Rect(10, yOffset + 20, 400, 20), "Grinding: " + isGrinding);
        
        if (isGrinding)
        {
            GUI.Label(new Rect(10, yOffset + 40, 400, 20), "Rail: " + (currentRail != null ? currentRail.gameObject.name : "None"));
            GUI.Label(new Rect(10, yOffset + 60, 400, 20), "Speed: " + currentSpeed.ToString("F1") + " / " + targetSpeed.ToString("F1"));
            GUI.Label(new Rect(10, yOffset + 80, 400, 20), "Position T: " + currentT.ToString("F3") + " | Direction: " + (movingForward ? "Forward" : "Backward"));
            GUI.Label(new Rect(10, yOffset + 100, 400, 20), "Height Offset: " + railHeightOffset.ToString("F2") + "m");
            GUI.Label(new Rect(10, yOffset + 120, 400, 20), "Boost: " + (isBoosting ? "ACTIVE" : "Ready") + " (" + boostTimer.ToString("F1") + "s)");
            GUI.Label(new Rect(10, yOffset + 140, 400, 20), "Grind Time: " + grindTime.ToString("F1") + "s");
            
            if (isSwitchingRail)
            {
                GUI.color = Color.yellow;
                GUI.Label(new Rect(10, yOffset + 160, 400, 20), "SWITCHING RAIL: " + (railSwitchTimer / railSwitchDuration * 100f).ToString("F0") + "%");
                GUI.color = Color.white;
            }
            
            if (currentRail != null)
            {
                float splineLength = currentRail.GetSplineLength();
                float distanceTraveled = currentT * splineLength;
                float distanceToEnd = movingForward ? (splineLength - distanceTraveled) : distanceTraveled;
                
                GUI.Label(new Rect(10, yOffset + 180, 400, 20), "Distance: " + distanceTraveled.ToString("F1") + "m / " + splineLength.ToString("F1") + "m");
                
                if (autoJumpAtEnd)
                {
                    GUI.color = distanceToEnd <= earlyJumpDistance ? Color.red : Color.white;
                    GUI.Label(new Rect(10, yOffset + 200, 400, 20), "Distance to End: " + distanceToEnd.ToString("F1") + "m (Jump at " + earlyJumpDistance.ToString("F1") + "m)");
                    GUI.color = Color.white;
                }
            }
            
            string adjacentInfo = "";
            if (currentRail != null && !isSwitchingRail)
            {
                if (currentRail.HasAdjacentRail(true)) adjacentInfo += "[A] Left ";
                if (currentRail.HasAdjacentRail(false)) adjacentInfo += "[D] Right";
            }
            if (!string.IsNullOrEmpty(adjacentInfo))
            {
                GUI.Label(new Rect(10, yOffset + 220, 400, 20), "Switch Rails: " + adjacentInfo);
            }
            
            GUI.Label(new Rect(10, yOffset + 240, 400, 20), "Switch Cooldown: " + railSwitchCooldownTimer.ToString("F2") + "s");
        }
        else
        {
            GUI.Label(new Rect(10, yOffset + 40, 400, 20), "Auto-Enter Radius: " + autoEnterRadius.ToString("F1") + "m");
            GUI.Label(new Rect(10, yOffset + 60, 400, 20), "Min Speed: " + minSpeedToEnter.ToString("F1"));
        }
    }

    /// <summary>
    /// Inicia particulas de grind (child do personagem)
    /// </summary>
    private void StartGrindParticles()
    {
        if (grindParticles == null) return;
        
        // Ativa o sistema de particulas
        if (!grindParticles.isPlaying)
        {
            grindParticles.Play();
            
            if (showDebugInfo)
            {
                Debug.Log("Particulas de grind iniciadas.");
            }
        }
    }
    
    /// <summary>
    /// Para as particulas de grind
    /// </summary>
    private void StopGrindParticles()
    {
        if (grindParticles == null) return;
        
        // Para o sistema de particulas
        if (grindParticles.isPlaying)
        {
            grindParticles.Stop();
            
            if (showDebugInfo)
            {
                Debug.Log("Particulas de grind paradas.");
            }
        }
    }
    

    
    /// <summary>
    /// Inicia particulas de switch grind (child do personagem)
    /// </summary>
    private void StartSwitchGrindParticles()
    {
        if (switchGrindParticles == null) return;
        
        // Ativa o sistema de particulas
        if (!switchGrindParticles.isPlaying)
        {
            switchGrindParticles.Play();
            
            if (showDebugInfo)
            {
                Debug.Log("Particulas de switch grind iniciadas.");
            }
        }
    }
    
    /// <summary>
    /// Para as particulas de switch grind
    /// </summary>
    private void StopSwitchGrindParticles()
    {
        if (switchGrindParticles == null) return;
        
        // Para o sistema de particulas
        if (switchGrindParticles.isPlaying)
        {
            switchGrindParticles.Stop();
            
            if (showDebugInfo)
            {
                Debug.Log("Particulas de switch grind paradas.");
            }
        }
    }
    
    /// <summary>
    /// Inicia particulas de boost (child do personagem)
    /// </summary>
    private void StartBoostParticles()
    {
        if (boostParticles == null) return;
        
        // Ativa o sistema de particulas
        if (!boostParticles.isPlaying)
        {
            boostParticles.Play();
            
            if (showDebugInfo)
            {
                Debug.Log("Particulas de boost iniciadas.");
            }
        }
    }
    
    /// <summary>
    /// Para as particulas de boost
    /// </summary>
    private void StopBoostParticles()
    {
        if (boostParticles == null) return;
        
        // Para o sistema de particulas
        if (boostParticles.isPlaying)
        {
            boostParticles.Stop();
            
            if (showDebugInfo)
            {
                Debug.Log("Particulas de boost paradas.");
            }
        }
    }
    
    public bool IsGrinding { get { return isGrinding; } }
    public float CurrentSpeed { get { return currentSpeed; } }
    public float GrindTime { get { return grindTime; } }
    public bool IsBoosting { get { return isBoosting; } }
}