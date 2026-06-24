# Alchemy Lab Desktop — Unity first-person edition

> Desktop-версия алхимической игры на Unity: управление от первого лица, мышь/клавиатура, центральный прицел, pickup через ПКМ, интерактивная книга рецептов, котёл, ингредиенты и эффекты рецептов. Ветка `dvizki-3` — это именно desktop-направление проекта.

[![Unity](https://img.shields.io/badge/Unity-6000.0.67f1-black?logo=unity)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-Gameplay%20Scripts-68217A?logo=csharp)](https://learn.microsoft.com/dotnet/csharp/)
[![URP](https://img.shields.io/badge/URP-17.0.4-blue)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/index.html)
[![Platform](https://img.shields.io/badge/Platform-Desktop%20%2F%20macOS-2ea44f)](#desktop-build)

## Что это за проект

`dvizki-3` — desktop-адаптация алхимической сцены. Главная идея сохранена: игрок берёт ингредиенты, кладёт их в котёл и запускает проверку рецепта ложкой. Отличие в управлении: вместо VR-контроллеров используется first-person режим, центральный прицел, мышь/тачпад и клавиатура.

Проект показывает, как одну механику можно перенести из VR-подхода в desktop-подход без разрушения базовой игровой логики.

## Игровой цикл

1. Игрок ходит по комнате от первого лица.
2. В центре экрана есть прицел.
3. Если прицел наведён на бутылку или ложку, ПКМ берёт предмет.
4. Повторное нажатие ПКМ отпускает предмет.
5. Бутылка при контакте с `IngredientZone` добавляется в котёл.
6. Рецепт не проверяется сразу после бутылок.
7. Проверка запускается только ложкой в `StirZone`.
8. Если рецепт правильный, срабатывает успешный эффект.
9. Если рецепт неправильный, котёл показывает fail-состояние.
10. Предметы возвращаются на стартовые места, чтобы игрок мог попробовать снова.

## Управление

| Действие | Клавиша / ввод |
|---|---|
| Перемещение | `WASD` |
| Обзор | мышь / тачпад |
| Поворот клавиатурой | `Q` / `E` |
| Взять или отпустить предмет | ПКМ |
| Взаимодействие с книгой | `E` или `F` |
| Листать книгу | `LeftArrow` / `RightArrow` |
| Закрыть книгу | `Escape` или `B` |
| Запустить движение Erika | `T` |

## Что реализовано

- first-person controller через `CharacterController`;
- desktop pickup-система через луч из центра камеры;
- удержание предмета перед игроком;
- переключение Rigidbody в kinematic-режим при удержании;
- автоматическая проверка контакта удерживаемого предмета с зонами котла;
- интерактивная UI-книга рецептов;
- prompt/прицел для взаимодействия;
- pause menu для desktop-сценария;
- data-driven ингредиенты и рецепты;
- эффекты успешных рецептов;
- build script для desktop/macOS;
- bootstrap, который автоматически добавляет нужные компоненты в сцену.

## Архитектура

```text
Assets/Scripts
├─ Desktop
│  ├─ DesktopGrabber.cs          # pickup через ПКМ и луч камеры
│  └─ DesktopSceneBootstrap.cs   # автонастройка desktop-компонентов
│
├─ Interaction
│  ├─ IInteractable.cs
│  ├─ PlayerInteraction.cs       # взаимодействие с книгой и объектами
│  ├─ PickupItem.cs
│  └─ InteractionPromptUI.cs     # центральный прицел / prompt
│
├─ Player
│  └─ FirstPersonCC.cs           # WASD, мышь, Q/E, raycast взаимодействий
│
├─ Crafting
│  ├─ CollectableItem.cs
│  ├─ CraftStorage.cs
│  ├─ RecipeChecker.cs
│  ├─ CauldronReactions.cs
│  ├─ CauldronIngredientZone.cs
│  ├─ CauldronStirZone.cs
│  ├─ StirringSpoon.cs
│  └─ RecipeBookViewer.cs
│
├─ UI
│  ├─ RecipeBookCanvasUI.cs
│  └─ DesktopPauseMenu.cs
│
└─ Effects
   └─ эффекты успешных и ошибочных рецептов
```

## Ключевые компоненты

### `DesktopSceneBootstrap`

Автоматически проверяет сцену после загрузки и добавляет нужные desktop-компоненты:

- `FirstPersonCC`;
- `PlayerInteraction`;
- `DesktopGrabber`;
- `ClickCollector`;
- `InteractionPromptUI`;
- `EventSystem` + `InputSystemUIInputModule`.

Это снижает риск, что сцена сломается из-за отсутствующего компонента после переноса из VR-версии.

### `DesktopGrabber`

Desktop-аналог VR-захвата:

- луч идёт из центра камеры;
- ПКМ работает как toggle;
- предмет удерживается перед камерой;
- физика временно переводится в kinematic;
- каждый кадр проверяется контакт бутылки с `IngredientZone`, а ложки — с `StirZone`.

### `FirstPersonCC`

Контроллер игрока:

- движение через `CharacterController`;
- обзор мышью/тачпадом;
- поворот через `Q/E`;
- raycast взаимодействия с книгой;
- использование `RaycastAll`, чтобы книга открывалась даже при наличии декоративных/невидимых коллайдеров.

## Desktop build

Ветка содержит editor build script:

```text
Assets/Editor/Build/BuildDesktopMac.cs
```

Для сборки открыть проект в Unity и использовать соответствующий editor menu/build flow. Основная сцена:

```text
Assets/Scenes/SampleScene.unity
```

## Как открыть

```bash
git clone --branch dvizki-3 https://github.com/pistaha/game-engines-projects.git
cd game-engines-projects
```

Дальше открыть папку в Unity Hub через Unity `6000.0.67f1`, дождаться импорта и запустить сцену:

```text
Assets/Scenes/SampleScene.unity
```

## Документация

- [`LAB_DEFENSE_DVIZKI3.md`](LAB_DEFENSE_DVIZKI3.md) — подробное описание desktop-версии, управления, архитектуры и сценария защиты.

## Что показывает проект

- Unity gameplay programming;
- перенос механики из VR в desktop;
- first-person control;
- interaction raycast architecture;
- pickup-систему без VR-контроллеров;
- ScriptableObject-driven recipes;
- UI-книгу рецептов;
- scene bootstrap automation;
- desktop build tooling;
- техническую документацию.

## Итог

`dvizki-3` — это desktop-кейс проекта Alchemy Lab. Он показывает, как VR-идею алхимической комнаты адаптировать под обычный компьютер: first-person камера, прицел, pickup через ПКМ, клавиатурное управление, UI-книга и та же data-driven логика рецептов.
