# Alchemy Lab — Unity desktop + VR game

> Большой Unity-проект про алхимическую комнату: desktop-версия игры, VR-версия для OpenXR/Quest, data-driven рецепты, интерактивная книга, котёл, ингредиенты, эффекты и editor-инструменты для настройки сцены и сборки APK.

[![Unity](https://img.shields.io/badge/Unity-6000.0.67f1-black?logo=unity)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-Gameplay%20Scripts-68217A?logo=csharp)](https://learn.microsoft.com/dotnet/csharp/)
[![XR](https://img.shields.io/badge/XR-OpenXR%20%2B%20XRI-blueviolet)](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/index.html)
[![Platform](https://img.shields.io/badge/Platform-Desktop%20%2F%20Meta%20Quest-2ea44f)](#vr--quest-build)

## Коротко

`dvizki-2` — это игровая лабораторная, доведённая до цельного Unity-кейса. Внутри есть две сильные версии одного проекта:

1. **Desktop game** — алхимическая комната с книгой рецептов, бутылками-ингредиентами, котлом, ложкой, проверкой рецептов и визуальными эффектами.
2. **VR / Quest edition** — та же игровая логика, адаптированная под взаимодействие руками: XR controllers, direct grab, ray interaction, world-space UI, OpenXR и сборка APK.

Проект не сводится к одной сцене. Здесь есть runtime-архитектура, ScriptableObject-данные, UI, эффекты рецептов, персонаж с анимациями, документация для защиты и editor automation.

## Gameplay loop

Игрок проходит понятный алхимический цикл:

1. Открывает книгу рецептов.
2. Выбирает рецепт и запоминает ингредиенты.
3. Берёт нужные бутылки.
4. Опускает их в зону котла.
5. Помешивает ложкой.
6. Система сравнивает состав с доступными рецептами.
7. При успехе запускает эффект рецепта, при ошибке очищает/возвращает состояние.

В VR-версии все ключевые действия выполняются через контроллеры: взять предмет, направить луч на UI, перелистнуть книгу, закрыть её, помешать ложкой.

## Что реализовано

- интерактивная алхимическая комната;
- data-driven ингредиенты через `IngredientData`;
- data-driven рецепты через `RecipeData`;
- проверка рецепта по составу без зависимости от порядка добавления;
- котёл с trigger-зонами для ингредиентов и помешивания;
- отдельная логика для бутылок и ложки;
- интерактивная книга рецептов с UI-страницами;
- визуальные эффекты успешных рецептов;
- персонаж Erika Archer с интерактивными анимациями;
- VR grab и ray interaction;
- world-space UI для XR;
- editor tools для настройки сцены;
- Quest APK build pipeline.

## Архитектура

```text
Assets/Scripts
├─ Crafting
│  ├─ CollectableItem.cs        # бутылка-ингредиент и связь с IngredientData
│  ├─ CraftStorage.cs           # текущее состояние крафта
│  ├─ RecipeChecker.cs          # проверка набора ингредиентов
│  ├─ CauldronReactions.cs      # реакция котла и запуск проверки
│  ├─ CauldronIngredientZone.cs # trigger-зона ингредиентов
│  ├─ CauldronStirZone.cs       # trigger-зона помешивания
│  ├─ StirringSpoon.cs          # ложка как отдельный интерактивный объект
│  └─ RecipeBookViewer.cs       # открытие, закрытие и навигация по книге
│
├─ Effects
│  ├─ RecipeEffectBase.cs
│  ├─ MainPotionEffect.cs
│  ├─ SlimeRecipeEffect.cs
│  ├─ FairyMistRecipeEffect.cs
│  ├─ StormBottleVolleyRecipeEffect.cs
│  ├─ PoseidonWaterfallRecipeEffect.cs
│  └─ другие эффекты рецептов
│
├─ UI
│  ├─ RecipeBookCanvasUI.cs
│  ├─ VRPauseMenu.cs
│  └─ VRAudioSettings.cs
│
├─ XR
│  ├─ VRGrabbableSetup.cs
│  ├─ VRTestSpawner.cs
│  └─ VrGrabTestCubeBootstrap.cs
│
└─ Player
   └─ FirstPersonCC.cs
```

### Основная идея архитектуры

Игровые правила отделены от способа управления. Desktop-версия и VR-версия используют одну и ту же основу крафта: ингредиенты, рецепты, storage, checker и reactions. VR добавляет другой слой взаимодействия, но не ломает игровую модель.

## Desktop version

Desktop-версия показывает базовый игровой сценарий и доменную логику проекта.

Ключевые элементы:

- `SampleScene` как основная сцена;
- алхимическая комната и набор предметов;
- книга рецептов как игровой объект и UI;
- добавление ингредиентов в котёл;
- помешивание ложкой;
- проверка рецепта;
- реакция на успех/ошибку;
- эффекты, подписанные на событие `RecipeChecker.OnRecipeSuccess`.

Почему это важно: проект построен не как одноразовый скрипт для демонстрации, а как расширяемая gameplay-система. Новые ингредиенты и рецепты можно добавлять через assets, а эффекты подключать к конкретным рецептам.

## VR / Quest edition

VR-версия переводит тот же игровой цикл в физическое взаимодействие руками.

Поддерживаются:

- XR Origin / XR Rig;
- left/right controllers;
- direct interactors для захвата предметов рядом с рукой;
- ray interactors для UI и удалённых объектов;
- `XRUIInputModule` для world-space UI;
- grabbable-объекты через `XR Grab Interactable`;
- XR Device Simulator для тестирования в редакторе;
- OpenXR runtime;
- Quest APK build.

## VR packages

Проект использует:

| Package | Version | Role |
|---|---:|---|
| Unity | `6000.0.67f1` | основная версия редактора |
| Universal Render Pipeline | `17.0.4` | рендеринг проекта |
| XR Interaction Toolkit | `3.0.10` | grab, ray, XR UI |
| OpenXR Plugin | `1.16.1` | VR runtime |
| Input System | `1.18.0` | action-based input |
| XR Mock HMD | `1.5.0-exp.3` | тестирование без физического шлема |

## Editor tools

Проект содержит отдельные editor-утилиты, чтобы настройка сцены была воспроизводимой.

```text
Assets/Editor
├─ RecipeBookBuilder.cs              # пересборка интерактивной книги
├─ RecipePhotoAssetRenderer.cs       # генерация изображений ингредиентов
├─ RecipeTabletPreviewCapture.cs     # превью книги/таблета
├─ ErikaAnimatorSetup.cs             # настройка персонажа
├─ AudioSetupTool.cs                 # настройка аудио
├─ Build/BuildQuestApk.cs            # сборка Quest APK
└─ VRSetup
   ├─ VRSceneSetupUtility.cs         # настройка XR для SampleScene
   ├─ VRGrabTestSceneBuilder.cs      # генерация тестовой grab-сцены
   └─ XRDeviceSimulatorSetupUtility.cs
```

### Главное меню в Unity

- `Tools > VR > Setup SampleScene VR Objects`
- `Tools > VR > Create Grab Test Scene`
- `Tools > VR > Add XR Device Simulator To SampleScene`
- `Tools > Build > Build Quest APK`
- `Tools > Recipe Book > Rebuild Interactive Book`

## Как открыть проект

1. Установить Unity `6000.0.67f1` с Android Build Support, если нужна Quest-сборка.
2. Клонировать репозиторий и ветку:

```bash
git clone --branch dvizki-2 https://github.com/pistaha/game-engines-projects.git
cd game-engines-projects
```

3. Открыть папку проекта в Unity Hub.
4. Дождаться импорта packages и компиляции scripts.
5. Открыть сцену:

```text
Assets/Scenes/SampleScene.unity
```

6. Нажать Play.

## VR setup

Если VR-объекты в сцене нужно пересобрать или проверить:

1. Открыть `Assets/Scenes/SampleScene.unity`.
2. Выполнить `Tools > VR > Setup SampleScene VR Objects`.
3. Проверить Console.
4. Сохранить сцену, если Unity сообщает, что настройки были изменены.

Для тестовой сцены:

1. Выполнить `Tools > VR > Create Grab Test Scene`.
2. Открыть `Assets/Scenes/VRGrabTestScene.unity`.
3. Запустить через подключённый headset или XR Device Simulator.

## VR / Quest build

Для сборки APK:

1. Убедиться, что установлен Android Build Support.
2. Проверить OpenXR в Project Settings.
3. Выполнить:

```text
Tools > Build > Build Quest APK
```

Скрипт `BuildQuestApk.cs` автоматически:

- добавляет `SampleScene` в Build Settings;
- настраивает Android package name `com.yarik.dvizki2`;
- включает IL2CPP;
- выставляет ARM64;
- использует ASTC texture compression;
- проверяет OpenXR loader;
- собирает APK в `Builds/Quest/dvizki2_quest_build.apk`.

## Документация внутри проекта

- [`Assets/Docs/LAB_DEFENSE_REPORT.md`](Assets/Docs/LAB_DEFENSE_REPORT.md) — подробный отчёт для защиты лабораторной работы.
- [`Assets/Docs/VR_SETUP_INSTRUCTIONS.md`](Assets/Docs/VR_SETUP_INSTRUCTIONS.md) — инструкция по VR grab setup, настройке XR и troubleshooting.

## Что демонстрирует проект

Этот репозиторий показывает сразу несколько компетенций:

- C# gameplay programming;
- Unity scene architecture;
- работа с ScriptableObject-данными;
- проектирование интерактивных игровых систем;
- UI внутри игрового мира;
- VR interaction design;
- OpenXR и XR Interaction Toolkit;
- editor tooling;
- сборка под Android/Quest;
- техническая документация и подготовка проекта к защите.

## Итог

`Alchemy Lab` — это не просто набор ассетов в Unity. Это цельный игровой проект с desktop- и VR-версией, разделённой архитектурой, воспроизводимой настройкой, документацией и сборочным контуром.

Главная ценность проекта — связность: рецепты, предметы, котёл, книга, эффекты, XR-взаимодействие и Quest build работают как одна система.
