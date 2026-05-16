using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(WaterDetector))]
public class WaterMovementController : MonoBehaviour
{
    [Header("Configurações de Velocidade")]
    public float speedThresholdToRunOnWater = 8.0f; // Velocidade mínima para correr na água
    public float speedThresholdToSwim = 5.0f;       // Velocidade abaixo da qual o personagem começa a nadar

    [Header("Configurações de Corrida na Água")]
    public float runOnWaterSpeedMultiplier = 1.2f; // Multiplicador de velocidade ao correr na água
    public float runOnWaterSurfaceOffset = 0.5f;   // Distância acima da superfície da água ao correr
    public float runOnWaterGravityMultiplier = 0.1f; // Gravidade reduzida ao correr na água

    [Header("Configurações de Natação")]
    public float swimSpeed = 3.0f;                 // Velocidade de natação
    public float swimUpForce = 5.0f;               // Força para nadar para cima
    public float swimDownForce = 3.0f;             // Força para nadar para baixo
    public float buoyancyForce = 9.81f;            // Força de empuxo (simula flutuação)

    private CharacterController _characterController;
    private WaterDetector _waterDetector;
    private Vector3 _moveDirection;
    private float _currentVerticalVelocity;

    private bool _isWaterRunning = false;
    private bool _isSwimming = false;

    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _waterDetector = GetComponent<WaterDetector>();
    }

    void Update()
    {
        // Obter a velocidade horizontal atual do personagem
        Vector3 horizontalVelocity = new Vector3(_characterController.velocity.x, 0, _characterController.velocity.z);
        float currentHorizontalSpeed = horizontalVelocity.magnitude;

        // Lógica de transição de estados
        if (_waterDetector.IsInsideWaterVolume)
        {
            if (currentHorizontalSpeed >= speedThresholdToRunOnWater && !_isSwimming)
            {
                _isWaterRunning = true;
                _isSwimming = false;
            }
            else if (currentHorizontalSpeed < speedThresholdToSwim && !_isWaterRunning)
            {
                _isSwimming = true;
                _isWaterRunning = false;
            }
            else if (!_isWaterRunning && !_isSwimming) // Caso inicial de entrada na água sem velocidade suficiente
            {
                _isSwimming = true;
            }
        }
        else
        {
            // Fora da água, resetar estados
            _isWaterRunning = false;
            _isSwimming = false;
        }

        // Aplicar movimentação baseada no estado atual
        if (_isWaterRunning)
        {
            HandleWaterRunning(currentHorizontalSpeed);
        }
        else if (_isSwimming)
        {
            HandleSwimming();
        }
        else
        {
            // Movimentação normal fora da água (gravidade padrão)
            ApplyGravity();
        }

        // Aplicar movimento ao CharacterController
        _characterController.Move(_moveDirection * Time.deltaTime);
    }

    private void HandleWaterRunning(float currentHorizontalSpeed)
    {
        // Manter o personagem na superfície da água
        Vector3 targetPosition = transform.position;
        targetPosition.y = _waterDetector.WaterSurfaceHeight + runOnWaterSurfaceOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f); // Suavizar a transição

        // Aplicar gravidade reduzida
        _currentVerticalVelocity += Physics.gravity.y * runOnWaterGravityMultiplier * Time.deltaTime;
        _moveDirection.y = _currentVerticalVelocity;

        // Manter a velocidade horizontal (assumindo que a entrada de movimento já está sendo aplicada)
        // Se você tiver um input de movimento separado, aplique-o aqui com runOnWaterSpeedMultiplier
    }

    private void HandleSwimming()
    {
        // Aplicar empuxo
        _currentVerticalVelocity += buoyancyForce * Time.deltaTime;

        // Movimento de natação (exemplo: WASD para horizontal, Space/Ctrl para vertical)
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 swimInput = transform.right * horizontalInput + transform.forward * verticalInput;
        _moveDirection = swimInput.normalized * swimSpeed;

        if (Input.GetKey(KeyCode.Space))
        {
            _currentVerticalVelocity = swimUpForce;
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            _currentVerticalVelocity = -swimDownForce;
        }
        else
        {
            // Reduzir velocidade vertical gradualmente se não houver input vertical
            _currentVerticalVelocity = Mathf.Lerp(_currentVerticalVelocity, 0, Time.deltaTime * 2f);
        }

        _moveDirection.y = _currentVerticalVelocity;
    }

    private void ApplyGravity()
    {
        if (_characterController.isGrounded)
        {
            _currentVerticalVelocity = -0.5f; // Pequena força para manter no chão
        }
        else
        {
            _currentVerticalVelocity += Physics.gravity.y * Time.deltaTime;
        }
        _moveDirection.y = _currentVerticalVelocity;
    }

    // Método para obter a velocidade atual do personagem (útil para outras lógicas)
    public float GetCurrentSpeed()
    {
        return new Vector3(_characterController.velocity.x, 0, _characterController.velocity.z).magnitude;
    }

    public bool IsWaterRunning() => _isWaterRunning;
    public bool IsSwimming() => _isSwimming;

    // Método para receber a velocidade horizontal inicial do PlayerMovement_FrontiersStyle
    public void SetInitialHorizontalVelocity(Vector3 velocity)
    {
        // Usar esta velocidade para iniciar o movimento na água, se aplicável
        // Por exemplo, para manter o momentum ao entrar na água correndo
        _moveDirection.x = velocity.x;
        _moveDirection.z = velocity.z;
    }
}
