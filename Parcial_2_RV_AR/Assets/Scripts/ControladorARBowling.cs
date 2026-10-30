using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;

public class ControladorARBowling : MonoBehaviour
{
    [Header("Manejadores AR")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    public GameObject punteroVisualPrefab;

    [Header("Modelos del Bowling")]
    public GameObject prefabPinos;
    public GameObject prefabBola;

    private GameObject punteroInstanciado;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private Pose posePuntero;
    private bool poseValida = false;
    private bool pistaCreada = false; // Bandera para saber si ya plantamos el juego

    // Variables de Interacción e Inercia (Extraídas del PDF de la clase)
    private GameObject objetoAgarrado;
    private Vector3 posAnterior;
    private Vector3 velocidadInercia;

    void Start()
    {
        punteroInstanciado = Instantiate(punteroVisualPrefab);
        punteroInstanciado.SetActive(false);
    }

    void Update()
    {
        ActualizarPuntero();

        if (Input.touchCount > 0)
        {
            Touch toque = Input.GetTouch(0);

            // Escudo UI para no instanciar al tocar botones
            if (EventSystem.current.IsPointerOverGameObject(toque.fingerId)) return;

            if (toque.phase == TouchPhase.Began)
                IniciarToque(toque.position);
            else if (toque.phase == TouchPhase.Moved && objetoAgarrado != null)
                Moviendo(toque.position);
            else if (toque.phase == TouchPhase.Ended && objetoAgarrado != null)
                Soltar();
        }
    }

    void ActualizarPuntero()
    {
        // Si ya creamos la pista, apagamos el puntero para siempre
        if (pistaCreada)
        {
            punteroInstanciado.SetActive(false);
            return;
        }

        var centro = new Vector2(Screen.width / 2, Screen.height / 2);

        // Usamos PlaneWithinPolygon para que detecte bien el plano invisible
        if (raycastManager.Raycast(centro, hits, TrackableType.PlaneWithinPolygon))
        {
            posePuntero = hits[0].pose;
            poseValida = true;
            punteroInstanciado.transform.SetPositionAndRotation(posePuntero.position, posePuntero.rotation);
            punteroInstanciado.SetActive(true);
        }
        else
        {
            poseValida = false;
            punteroInstanciado.SetActive(false);
        }
    }

    void IniciarToque(Vector2 pos)
    {
        Ray rayo = Camera.main.ScreenPointToRay(pos);

        // AGARRAR: Validamos que el rayo choque y que ese objeto TENGA Rigidbody (ignora el piso)
        if (Physics.Raycast(rayo, out RaycastHit hitFisico) && hitFisico.collider.GetComponent<Rigidbody>() != null)
        {
            objetoAgarrado = hitFisico.collider.gameObject;
            objetoAgarrado.GetComponent<Rigidbody>().isKinematic = true;
            posAnterior = objetoAgarrado.transform.position;
        }
        // INSTANCIAR PISTA Y BOLA: Si no agarramos nada, tocamos la pantalla y la pose es válida
        else if (poseValida && !pistaCreada)
        {
            // 1. Instanciamos los Pinos donde apunta el puntero
            Instantiate(prefabPinos, posePuntero.position, posePuntero.rotation);

            // 2. Instanciamos la bola un poco más cerca de la cámara (ej: 0.8 metros atrás de los pinos)
            Vector3 posBola = posePuntero.position - Camera.main.transform.forward * 0.8f;
            posBola.y += 0.1f;
            Instantiate(prefabBola, posBola, Quaternion.identity);

            pistaCreada = true;

            // CONGELAR EL PISO: Apagamos el escáner al crear el primer objeto para estabilizar físicas
            if (planeManager != null && planeManager.enabled)
            {
                planeManager.enabled = false;
                Debug.Log("Escáner apagado. Piso congelado.");
            }
        }
    }

    void Moviendo(Vector2 pos)
    {
        if (raycastManager.Raycast(pos, hits, TrackableType.PlaneWithinPolygon))
        {
            Vector3 nuevaPos = hits[0].pose.position;
            nuevaPos.y += 0.1f;

            objetoAgarrado.transform.position = nuevaPos;

            // Calculamos la inercia para lanzarlo
            velocidadInercia = (nuevaPos - posAnterior) / Time.deltaTime;
            posAnterior = nuevaPos;
        }
    }

    void Soltar()
    {
        Rigidbody rb = objetoAgarrado.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = velocidadInercia; // Le aplicamos la fuerza del lanzamiento
        }
        objetoAgarrado = null;
    }
}