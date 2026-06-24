# Alchemy Lab VR — Unity OpenXR / Quest edition

> VR-версия алхимической игры на Unity: игрок берёт бутылки контроллерами, открывает книгу рецептов, добавляет ингредиенты в котёл, помешивает ложкой и получает реакцию рецепта. Ветка `dvizki-2` — это именно VR-направление проекта.

[![Unity](https://img.shields.io/badge/Unity-6000.0.67f1-black?logo=unity)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-Gameplay%20Scripts-68217A?logo=csharp)](https://learn.microsoft.com/dotnet/csharp/)
[![XR](https://img.shields.io/badge/XR-OpenXR%20%2B%20XRI-blueviolet)](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/index.html)
[![Quest](https://img.shields.io/badge/Build-Meta%20Quest%20APK-2ea44f)](#quest-apk-build)

## Что это за проект

`dvizki-2` — VR-версия алхимической комнаты. Здесь базовая игровая механика крафта перенесена в физическое взаимодействие: предметы берутся руками через XR-контроллеры, UI книги работает в VR, а проверка рецепта запускается через действие в сцене — помешивание ложкой в котле.

Основная задача проекта — показать, как gameplay-система может работать в VR: не только через обычные клики, а через grab, ray interaction, world-space UI, OpenXR input и отдельную сборку под Quest.

## Игровой цикл

1. Игрок находится в алхимической комнате.
2. Берёт книгу рецептов VR-контроллером.
3. Перелистывает страницы через стик, кнопки `A/X` или UI-кнопки.
4. Выбирает рецепт и ищет нужные бутылки.
5. Берёт бутылки direct grab-контроллерами.
6. Опускает ингредиенты в зону котла.
7. Берёт ложку и помешивает в `StirZone`.
8. `RecipeChecker` проверяет состав.
9. При успехе запускается эффект рецепта, при ошибке котёл показывает fail-состояние.
10. Состояние очищается/возвращается для следующей попытки.

## Основные возможности

- VR-взаимодействие через XR Interaction Toolkit;
- direct grab для близких предметов;
- ray interaction для UI и удалённых элементов;
- world-space Canvas и `XRUIInputModule`;
- grabbable-бутылки с `CollectableItem` и `IngredientData`;
- отдельная ложка `StirringSpoon`, которая не считается ингредиентом;
- книга рецептов с открытием, закрытием и перелистыванием;
- data-driven рецепты на `RecipeData`;
- эффекты успешных рецептов через событие `RecipeChecker.OnRecipeSuccess`;
- Quest APK build script;
- VR setup utilities для воспроизводимой настройки сцены.

## Архитектура

```text
Assets/Scripts
├─ Crafting
│  ├─ CollectableItem.cs        # бутылка и IngredientData
│  ├─ CraftStorage.cs           # текущее состояние крафта
│  ├─ RecipeChecker.cs          # проверка рецептов
│  ├─ CauldronReactions.cs      # логика котла
│  ├─ CauldronIngredientZone.cs # trigger-зона ингредиентов
│  ├─ CauldronStirZone.cs       # trigger-зона ложки
│  ├─ StirringSpoon.cs          # отдельный инструмент помешивания
│  └─ RecipeBookViewer.cs       # открытие и навигация по книге
│
├─ Effects
│  └─ визуальные реакции рецептов
│
├─ UI
│  ├─ RecipeBookCanvasUI.cs
│  ├─ VRPauseMenu.cs
│  └─ VRAudioSettings.cs
│
└─ XR
   ├─ VRGrabbableSetup.cs
   ├─ VRTestSpawner.cs
   └─ VrGrabTestCubeBootstrap.cs
```

## XR / VR стек

| Компонент | Версия / роль |
|---|---|
| Unity | `6000.0.67f1` |
| Universal Render Pipeline | `17.0.4` |
| XR Interaction Toolkit | `3.0.10` |
| OpenXR Plugin | `1.16.1` |
| Input System | `1.18.0` |
| XR Mock HMD | `1.5.0-exp.3` |

## Editor-инструменты

```text
Assets/Editor
├─ Build/BuildQuestApk.cs
├─ VRSetup/VRSceneSetupUtility.cs
├─ VRSetup/VRGrabTestSceneBuilder.cs
├─ VRSetup/XRDeviceSimulatorSetupUtility.cs
├─ RecipeBookBuilder.cs
├─ RecipePhotoAssetRenderer.cs
└─ ErikaAnimatorSetup.cs
```

Главные команды Unity:

- `Tools > VR > Setup SampleScene VR Objects`
- `Tools > VR > Create Grab Test Scene`
- `Tools > VR > Add XR Device Simulator To SampleScene`
- `Tools > Build > Build Quest APK`
- `Tools > Recipe Book > Rebuild Interactive Book`

## Как открыть

```bash
git clone --branch dvizki-2 https://github.com/pistaha/game-engines-projects.git
cd game-engines-projects
```

Дальше открыть папку в Unity Hub через Unity `6000.0.67f1`, дождаться импорта и запустить:

```text
Assets/Scenes/SampleScene.unity
```

## Quest APK build

Для сборки APK нужен Android Build Support в Unity.

В Unity выполнить:

```text
Tools > Build > Build Quest APK
```

Скрипт автоматически:

- добавляет `SampleScene` в Build Settings;
- настраивает package name `com.yarik.dvizki2`;
- включает IL2CPP;
- выставляет ARM64;
- использует ASTC compression;
- проверяет OpenXR loader;
- собирает APK в `Builds/Quest/dvizki2_quest_build.apk`.

## Документация

- [`Assets/Docs/LAB_DEFENSE_REPORT.md`](Assets/Docs/LAB_DEFENSE_REPORT.md) — подробный отчёт по VR-версии.
- [`Assets/Docs/VR_SETUP_INSTRUCTIONS.md`](Assets/Docs/VR_SETUP_INSTRUCTIONS.md) — настройка XR, grab, simulator и troubleshooting.

## Что показывает проект

- Unity gameplay programming;
- VR interaction design;
- OpenXR и XR Interaction Toolkit;
- физическое взаимодействие с предметами;
- world-space UI;
- ScriptableObject-driven data;
- editor tooling;
- Quest/Android build pipeline;
- техническую документацию для защиты.

## Итог

`dvizki-2` — это VR-кейс проекта Alchemy Lab. Он показывает, как одну игровую механику превратить в VR-сценарий с руками, контроллерами, UI в пространстве, OpenXR-настройкой и сборкой под Quest.
