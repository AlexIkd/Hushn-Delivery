using UnityEngine;

public class WaterDetector : MonoBehaviour
{
    public LayerMask waterLayer; // Camada da água
    public float detectionHeight = 1.0f; // Altura acima do personagem para iniciar o Raycast
    public float maxDetectionDistance = 2.0f; // Distância máxima para detectar a água abaixo do personagem

    private bool _isInsideWaterVolume = false;
    private float _waterSurfaceHeight = 0.0f;

    public bool IsInsideWaterVolume => _isInsideWaterVolume;
    public float WaterSurfaceHeight => _waterSurfaceHeight;

    void Update()
    {
        DetectWater();
    }

    private void DetectWater()
    {
        // Ponto de origem do Raycast (um pouco acima do personagem para garantir detecção)
        Vector3 rayOrigin = transform.position + Vector3.up * detectionHeight;

        // Desenha o Raycast no editor para depuração
        Debug.DrawRay(rayOrigin, Vector3.down * (detectionHeight + maxDetectionDistance), Color.blue);

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, detectionHeight + maxDetectionDistance, waterLayer))
        {
            _isInsideWaterVolume = true;
            _waterSurfaceHeight = hit.point.y; // A altura da superfície da água é o ponto de impacto do Raycast
        }
        else
        {
            _isInsideWaterVolume = false;
            _waterSurfaceHeight = 0.0f; // Resetar se não houver água detectada
        }
    }

    // Opcional: Usar OnTriggerEnter/Exit para volumes de água maiores ou mais complexos
    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & waterLayer) != 0)
        {
            // Se o personagem entrar em um volume de água (trigger)
            // Esta lógica pode ser usada em conjunto ou como alternativa ao Raycast
            // Para simplificar, vamos focar no Raycast para a altura da superfície.
            // _isInsideWaterVolume = true; // Pode ser definido aqui se preferir
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & waterLayer) != 0)
        {
            // Se o personagem sair de um volume de água (trigger)
            // _isInsideWaterVolume = false; // Pode ser definido aqui se preferir
        }
    }
}
