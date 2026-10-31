using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class RocketLaunchSequence : MonoBehaviour
{
    // --- Ссылки на объекты сцены ---
    [Header("Эффекты и Объекты")]
    public ParticleSystem leftTurbineEffect;
    public ParticleSystem rightTurbineEffect;
    public GameObject playerObject;
    public Canvas playerCanvas;
    public Transform rocketShip;

    [Header("Двери и Створки")]
    public Transform entryDoor;
    public Transform leftHangarDoor;
    public Transform rightHangarDoor;

    [Header("Камеры")]
    public Camera playerCamera;
    public Camera mainCamera;
    public Camera mainCamera2;

    [Header("Звуки")]
    public AudioSource rocketEngineAudioSource;
    public AudioClip entryDoorCloseSound;
    public AudioClip hangarDoorOpenSound;
    public AudioClip hangarDoorCloseSound;
    public AudioClip fullLaunchRoarSound;

    [Header("Параметры Анимаций")]
    public float doorAnimationSpeed = 90.0f;
    public float doorCloseRotationY = 90.0f;
    public float hangarDoorOpenOffset = 5.0f;
    public float hangarDoorSpeed = 2.0f;
    public float rocketFlySpeed = 20.0f;

    [Header("Тайминги Последовательности (Задержки)")]
    public float delayAfterDoorClose = 1.0f;
    public float delayAfterHangarOpens = 1.5f;
    public float delayBeforeLaunch = 2.0f;
    public float delayBeforeHangarClose = 4.0f;
    [Tooltip("Через сколько секунд после СТАРТА ракеты звук двигателя прекратится. Установите 0, чтобы не выключать.")]
    public float engineSoundStopTime = 12.0f;

    [Header("Сцена Атаки")]
    public GameObject enemyMissilePrefab;
    public GameObject explosionEffectPrefab;
    public Canvas creditsCanvas;
    public AudioClip missileLaunchSound;
    public AudioClip explosionSound;
    [Tooltip("Задержка перед запуском вражеской ракеты после переключения на 3-ю камеру")]
    public float delayBeforeMissileLaunch = 2.0f;
    public float missileSpeed = 50.0f;
    [Tooltip("Сколько секунд будут показываться титры перед выходом в меню")]
    public float creditsDuration = 5.0f;

    // --- Приватные переменные ---
    private AudioSource managerAudioSource;
    private float initialStartSpeedLeft, initialStartSizeLeft, initialEmissionRateLeft;
    private float initialStartSpeedRight, initialStartSizeRight, initialEmissionRateRight;
    private Vector3 leftHangarDoor_InitialPos, rightHangarDoor_InitialPos;

    // *** ИЗМЕНЕНИЕ: Флаг для остановки движения корабля при попадании ***
    private bool isShipHit = false;


    void Start()
    {
        managerAudioSource = GetComponent<AudioSource>();

        var mainLeft = leftTurbineEffect.main;
        initialStartSpeedLeft = mainLeft.startSpeed.constant;
        initialStartSizeLeft = mainLeft.startSize.constant;
        initialEmissionRateLeft = leftTurbineEffect.emission.rateOverTime.constant;

        var mainRight = rightTurbineEffect.main;
        initialStartSpeedRight = mainRight.startSpeed.constant;
        initialStartSizeRight = mainRight.startSize.constant;
        initialEmissionRateRight = rightTurbineEffect.emission.rateOverTime.constant;

        leftHangarDoor_InitialPos = leftHangarDoor.localPosition;
        rightHangarDoor_InitialPos = rightHangarDoor.localPosition;

        leftTurbineEffect.Stop();
        rightTurbineEffect.Stop();

        playerCamera.gameObject.SetActive(true);
        mainCamera.gameObject.SetActive(false);
        mainCamera2.gameObject.SetActive(false);

        if (creditsCanvas != null) creditsCanvas.gameObject.SetActive(false);
    }

    public void StartLaunchSequence()
    {
        StartCoroutine(LaunchCoroutine());
    }

    private IEnumerator LaunchCoroutine()
    {
        // ... (код до взлета остается без изменений) ...

        playerObject.SetActive(false);
        if (playerCanvas != null) playerCanvas.gameObject.SetActive(false);

        mainCamera.gameObject.SetActive(true);
        yield return null;
        playerCamera.gameObject.SetActive(false);

        managerAudioSource.PlayOneShot(entryDoorCloseSound);
        Quaternion targetRotation = Quaternion.Euler(entryDoor.localEulerAngles.x, doorCloseRotationY, entryDoor.localEulerAngles.z);
        while (Quaternion.Angle(entryDoor.localRotation, targetRotation) > 0.01f)
        {
            entryDoor.localRotation = Quaternion.RotateTowards(entryDoor.localRotation, targetRotation, doorAnimationSpeed * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(delayAfterDoorClose);

        managerAudioSource.PlayOneShot(hangarDoorOpenSound);
        Vector3 leftTargetPos = leftHangarDoor_InitialPos - new Vector3(hangarDoorOpenOffset, 0, 0);
        Vector3 rightTargetPos = rightHangarDoor_InitialPos + new Vector3(hangarDoorOpenOffset, 0, 0);
        while (Vector3.Distance(leftHangarDoor.localPosition, leftTargetPos) > 0.01f)
        {
            leftHangarDoor.localPosition = Vector3.MoveTowards(leftHangarDoor.localPosition, leftTargetPos, Time.deltaTime * hangarDoorSpeed);
            rightHangarDoor.localPosition = Vector3.MoveTowards(rightHangarDoor.localPosition, rightTargetPos, Time.deltaTime * hangarDoorSpeed);
            yield return null;
        }

        yield return new WaitForSeconds(delayAfterHangarOpens);

        SetTurbinePower(0.5f);
        if (rocketEngineAudioSource != null)
        {
            rocketEngineAudioSource.loop = true;
            rocketEngineAudioSource.Play();
        }
        leftTurbineEffect.Play();
        rightTurbineEffect.Play();

        yield return new WaitForSeconds(delayBeforeLaunch);

        SetTurbinePower(1.0f);
        managerAudioSource.PlayOneShot(fullLaunchRoarSound);

        if (engineSoundStopTime > 0)
        {
            StartCoroutine(StopEngineSoundCoroutine());
        }

        StartCoroutine(CloseHangarDoorsCoroutine());

        // *** ИЗМЕНЕНИЕ: Цикл движения теперь зависит от флага isShipHit ***
        while (rocketShip != null && !isShipHit)
        {
            rocketShip.Translate(Vector3.forward * rocketFlySpeed * Time.deltaTime, Space.World);
            yield return null;
        }
    }

    private IEnumerator StopEngineSoundCoroutine()
    {
        yield return new WaitForSeconds(engineSoundStopTime);
        if (rocketEngineAudioSource != null)
        {
            rocketEngineAudioSource.Stop();
        }
    }

    private IEnumerator CloseHangarDoorsCoroutine()
    {
        // ... (код до переключения камер без изменений) ...
        yield return new WaitForSeconds(delayBeforeHangarClose);
        managerAudioSource.PlayOneShot(hangarDoorCloseSound);
        while (Vector3.Distance(leftHangarDoor.localPosition, leftHangarDoor_InitialPos) > 0.01f)
        {
            leftHangarDoor.localPosition = Vector3.MoveTowards(leftHangarDoor.localPosition, leftHangarDoor_InitialPos, Time.deltaTime * hangarDoorSpeed);
            rightHangarDoor.localPosition = Vector3.MoveTowards(rightHangarDoor.localPosition, rightHangarDoor_InitialPos, Time.deltaTime * hangarDoorSpeed);
            yield return null;
        }
        mainCamera2.gameObject.SetActive(true);
        RenderSettings.fogDensity = 0f;
        yield return null;
        mainCamera.gameObject.SetActive(false);

        StartCoroutine(AttackSequenceCoroutine());
    }

    private IEnumerator AttackSequenceCoroutine()
    {
        yield return new WaitForSeconds(delayBeforeMissileLaunch);

        Vector3 spawnPosition = mainCamera2.transform.position - mainCamera2.transform.up * 5.0f;
        GameObject missile = Instantiate(enemyMissilePrefab, spawnPosition, Quaternion.identity);

        managerAudioSource.PlayOneShot(missileLaunchSound);

        while (missile != null && rocketShip != null)
        {
            missile.transform.LookAt(rocketShip.position);
            missile.transform.Translate(Vector3.forward * missileSpeed * Time.deltaTime);

            if (Vector3.Distance(missile.transform.position, rocketShip.position) < 2.0f)
            {
                // *** ИЗМЕНЕНИЕ: Останавливаем корабль и передаем его в метод взрыва ***
                isShipHit = true; // Устанавливаем флаг, чтобы остановить движение
                TriggerExplosion(rocketShip); // Передаем сам Transform корабля

                Destroy(missile);
                yield break;
            }

            yield return null;
        }
    }

    // *** ИЗМЕНЕНИЕ: Метод теперь принимает Transform корабля для уничтожения ***
    void TriggerExplosion(Transform shipToDestroy)
    {
        // Создаем эффект взрыва в позиции корабля
        Instantiate(explosionEffectPrefab, shipToDestroy.position, Quaternion.identity);

        managerAudioSource.PlayOneShot(explosionSound);
        if (rocketEngineAudioSource != null && rocketEngineAudioSource.isPlaying)
        {
            rocketEngineAudioSource.Stop();
        }

        // *** ИЗМЕНЕНИЕ: Надежное и мгновенное удаление корабля ***
        // Сначала делаем невидимым, потом уничтожаем
        shipToDestroy.gameObject.SetActive(false);
        Destroy(shipToDestroy.gameObject);

        StartCoroutine(EndSequenceCoroutine());
    }

    private IEnumerator EndSequenceCoroutine()
    {
        if (creditsCanvas != null)
        {
            creditsCanvas.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(creditsDuration);

        SceneManager.LoadScene("MainMenu");
    }


    private void SetTurbinePower(float multiplier)
    {
        var mainLeft = leftTurbineEffect.main;
        mainLeft.startSpeed = initialStartSpeedLeft * multiplier;
        mainLeft.startSize = initialStartSizeLeft * multiplier;
        var emissionLeft = leftTurbineEffect.emission;
        emissionLeft.rateOverTime = initialEmissionRateLeft * multiplier;

        var mainRight = rightTurbineEffect.main;
        mainRight.startSpeed = initialStartSpeedRight * multiplier;
        mainRight.startSize = initialStartSizeRight * multiplier;
        var emissionRight = rightTurbineEffect.emission;
        emissionRight.rateOverTime = initialEmissionRateRight * multiplier;
    }
}