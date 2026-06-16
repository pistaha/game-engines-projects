using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

// Автоматически подготавливает сцену под обычную десктопную версию без виртуальной реальности.
public static class DesktopSceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigureDesktopScene()
    {
        // Этот метод вызывается движком после загрузки сцены и добавляет всё,
        // что нужно для десктопного управления, если это не настроено в самой сцене.
        EnsureDesktopPlayer();
        EnsureDesktopEventSystem();
        EnsureDesktopPauseMenu();
        DisableLegacyWorldBookText();
        EnsureCauldronFailEffects();
    }

    private static void EnsureDesktopPlayer()
    {
        FirstPersonCC player = Object.FindFirstObjectByType<FirstPersonCC>(FindObjectsInactive.Include);
        if (player == null)
        {
            GameObject playerObject = GameObject.Find("Player");
            if (playerObject == null)
                return;

            player = playerObject.AddComponent<FirstPersonCC>();
        }

        if (player.GetComponent<ClickCollector>() == null)
            player.gameObject.AddComponent<ClickCollector>();

        if (player.GetComponent<InteractionPromptUI>() == null)
            player.gameObject.AddComponent<InteractionPromptUI>();

        PlayerInteraction playerInteraction = player.GetComponent<PlayerInteraction>();
        if (playerInteraction == null)
            playerInteraction = player.gameObject.AddComponent<PlayerInteraction>();

        DesktopGrabber desktopGrabber = player.GetComponent<DesktopGrabber>();
        if (desktopGrabber == null)
            desktopGrabber = player.gameObject.AddComponent<DesktopGrabber>();

        // ПКМ обрабатывает скрипт захвата: первый клик берёт предмет,
        // второй клик отпускает. Отдельный скрипт взаимодействия остаётся для клавиатуры.
        playerInteraction.SetLeftMouseInteractEnabled(false);
        desktopGrabber.enabled = true;

        player.enabled = true;
    }

    private static void EnsureDesktopEventSystem()
    {
        EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        EventSystem primary = null;

        for (int i = 0; i < eventSystems.Length; i++)
        {
            if (eventSystems[i] == null)
                continue;

            if (primary == null)
                primary = eventSystems[i];
            else
                eventSystems[i].gameObject.SetActive(false);
        }

        if (primary == null)
            primary = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();

        primary.gameObject.SetActive(true);
        primary.enabled = true;

        InputSystemUIInputModule inputModule = primary.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
            inputModule = primary.gameObject.AddComponent<InputSystemUIInputModule>();

        inputModule.enabled = true;

        Behaviour[] modules = primary.GetComponents<Behaviour>();
        for (int i = 0; i < modules.Length; i++)
        {
            Behaviour module = modules[i];
            if (module == null || module == primary || module == inputModule)
                continue;

            string typeName = module.GetType().Name;
            if (typeName.Contains("StandaloneInputModule"))
                module.enabled = false;
        }
    }

    private static void EnsureDesktopPauseMenu()
    {
        if (Object.FindFirstObjectByType<DesktopPauseMenu>(FindObjectsInactive.Include) != null)
            return;

        new GameObject("DesktopPauseMenu").AddComponent<DesktopPauseMenu>();
    }

    private static void EnsureCauldronFailEffects()
    {
        RecipeChecker[] recipeCheckers = Object.FindObjectsByType<RecipeChecker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < recipeCheckers.Length; i++)
        {
            RecipeChecker recipeChecker = recipeCheckers[i];
            if (recipeChecker == null)
                continue;

            CauldronFailEffect failEffect = recipeChecker.GetComponent<CauldronFailEffect>();
            if (failEffect == null)
                failEffect = recipeChecker.gameObject.AddComponent<CauldronFailEffect>();

            if (failEffect.cauldronRenderer == null)
                failEffect.cauldronRenderer = recipeChecker.GetComponentInChildren<Renderer>();

            if (failEffect.cauldronLight == null)
                failEffect.cauldronLight = recipeChecker.GetComponentInChildren<Light>();
        }
    }

    private static void DisableLegacyWorldBookText()
    {
        // Старые 3D-страницы книги больше не используются: десктопная книга
        // открывается как экранное UI, поэтому старый TMP-текст выключается.
        TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null)
                continue;

            if (text.name.Contains("RecipeBookHint") || text.GetComponentInParent<RecipeBookViewer>() != null)
                text.gameObject.SetActive(false);
        }
    }
}
