# Darkest — 다키스트 던전류 턴제 로그라이크 (Unity 6000.3.13f1)

## 0. 요구사항 3줄 요약

| | |
|---|---|
| **목표** | 4인 파티 진형 전투 + 스트레스 + 던전 탐험 + 마을 관리가 한 바퀴 도는 MVP |
| **입력** | 마우스 클릭(uGUI 버튼), 콘텐츠 데이터(코드 더미 또는 ScriptableObject), seed |
| **출력** | 화면(마을/던전/전투/결과), 전투 로그, 로컬 세이브 파일 |

### 가정 (Assumptions)
- 아트/사운드는 더미. `Resources/Darkest/Placeholders/*.png`를 같은 이름으로 덮어쓰면 교체된다.
- UI는 프리팹 없이 코드로 생성(uGUI + 내장 `LegacyRuntime.ttf`). 씬에는 `GameBootstrap` 하나만 있으면 된다.
- 입력: Input System 패키지가 있으면 `InputSystemUIInputModule`, 없으면 `StandaloneInputModule`을 자동 선택(asmdef versionDefines).
- 저장은 **마을**과 **던전 탐험 중(전투 밖)**에서 가능. 전투 중 저장은 확장 범위. 던전 저장에는 상태이상과 난수 상태가 빠진다.
- 기존 저장소 루트의 2022.3 프로젝트와 섞지 않도록 `Darkest/` 폴더에 **독립 Unity 프로젝트**로 만들었다.

### 범위
| MVP (이번 구현) | 확장 (제안) |
|---|---|
| 4:4 진형 전투, 열(rank) 기반 스킬 사용/타격 규칙 | 시체(corpse), 표식(mark), 버프/디버프 스킬, 은신, 전투 종료 연출 대기 |
| 명중/회피/치명/방어, 출혈·역병·기절, 밀치기/당기기 | 몬스터 크기(2칸), 보스 전용 AI·페이즈 |
| 죽음의 문턱 + 치명상 저항, 영웅 경험치/레벨(Lv0~5, 능력치 상승) | 장비/장신구, 기벽(quirk)/질병 |
| 스트레스, 결의 판정(고통/미덕), 고통 폭주·미덕 격려, 심장마비 | 고통 시 명령 거부/자해 등 행동 이상 |
| 던전 자동 생성(메인 경로+곁가지), 빛/횃불, 허기, 골동품, 보물, 보스 | 야영 스킬(직업별), 함정/정찰, 복도 조우 |
| 빛 보정(어두울수록 몬스터 명중/치명↑, 전리품↑), 야영(장작+식량 → 회복/스트레스 완화) | |
| 전투 연출 큐(피격 흔들림, 데미지/회복/상태 숫자), 연출 중 입력 잠금 | 스킬 애니메이션, 사운드, 카메라 연출 |
| 마을: 명부, 모집(역마차), 선술집, 보급, 해고, 귀환 정산 | 건물 업그레이드, 요양원, 대장간, 주간 이벤트 |
| 로컬 세이브/로드(마을 + 던전 탐험 중), seed 재현 | 전투 중 저장, 클라우드 세이브, 리플레이 뷰어 |

---

## 1. 실행 방법

1. Unity Hub → **Add project from disk** → 이 `Darkest/` 폴더 선택 (에디터 6000.3.13f1).
2. `Assets/Darkest/Scenes/Main.unity` 열고 **Play**.
   - 씬이 열리지 않거나 깨져 있으면 메뉴 **Darkest → Create Main Scene**.
3. (선택) 메뉴 **Darkest → Generate Content Assets** → `Assets/Darkest/Data/`에 SO 에셋 생성
   → `GameBootstrap`의 **Content** 슬롯에 `ContentDatabase`, **Balance** 슬롯에 `BalanceConfig`를 넣으면
   이후 기획 수치는 에셋에서 수정한다. 비워두면 코드 더미(`DefaultContent`)와 기본 밸런스로 동작.

### 플레이 흐름
마을(영웅 클릭 순서대로 1~4열 편성, 식량/횃불/장작 구매) → **던전 출발** → 인접 방으로 이동(복도마다 빛 감소·스트레스)
→ 정리된 방에서 **야영**·**저장** 가능 → 전투방에서 **스킬 → 대상** 선택(연출이 끝나면 다음 입력, 영웅 차례에 **도주** 75%) → 전투 결과 패널 → 보스 처치/후퇴/전멸 → 결과 → **마을로 귀환**(사망자 제거, 경험치·레벨업, 전리품 입금, 주차 +1, 자동 저장).

### `GameBootstrap` 인스펙터
| 필드 | 설명 |
|---|---|
| Content / Balance | SO 데이터. 비우면 코드 기본값 |
| Use Random Seed / Fixed Seed | 끄면 고정 seed → **같은 입력이면 같은 결과**(버그 재현용) |
| Log Level | Verbose(상태 전이까지) ~ None. 릴리즈는 Warning 권장 |
| Load Save On Start | 시작 시 세이브 자동 로드 |
| Debug View | 비우면 자동 추가. 인스펙터에서 seed/모드/라운드/현재 행동자를 읽기 전용으로 표시 |

`GameDebugView` 우클릭 메뉴: **Dump Battle History**(seed + 명령 기록), **Dump Dungeon Map**.
씬 뷰에서 `BattleStage` 오브젝트를 보면 Gizmos로 열 위치(청록=영웅, 자홍=몬스터), 현재 행동자(노랑), 선택 가능한 대상(빨강)이 보인다.

### Unity 없이 테스트
```bash
dotnet test Tools/DomainTests      # 142개 EditMode 테스트 (도메인만 컴파일)
```
같은 테스트가 Unity **Test Runner → EditMode**에서도 그대로 돈다(`Darkest.Tests.EditMode.asmdef`).

---

## 2. 폴더 구조

```
Darkest/
├─ Assets/Darkest/
│  ├─ Runtime/
│  │  ├─ Domain/                  ← 순수 C# (Darkest.Domain.asmdef, noEngineReferences=true)
│  │  │  ├─ Core/                 로그 인터페이스, IRandom(seed), Guard, EventBus
│  │  │  ├─ Combat/               Combatant/Hero/Monster, Formation(1~4열)
│  │  │  │  ├─ Stats/  Skills/  Status/  Resolution/   스탯, 스킬+효과(Strategy), 상태이상, 명중/피해(Strategy)
│  │  │  │  ├─ Commands/          UseSkill/Move/Pass (Command)
│  │  │  │  ├─ Flow/              BattleSession(Facade) + BattleStates(State) + 턴 순서(Strategy)
│  │  │  │  ├─ AI/                IEnemyAI (Strategy)
│  │  │  │  └─ Events/            전투 도메인 이벤트
│  │  │  ├─ Stress/               StressMeter, StressService, 고통/미덕 정의
│  │  │  ├─ Dungeon/              DungeonGenerator, Expedition, CorridorTravel, Camp, LightEffects, TorchLight, Curio
│  │  │  ├─ Hamlet/               Town, TownService (모집/선술집/보급/정산)
│  │  │  ├─ Content/              정의(Definition) 타입, IContentCatalog, CombatantFactory, DefaultContent(더미)
│  │  │  ├─ Save/                 GameSaveData, LineSaveSerializer, SaveMapper, SaveGateway, ISaveRepository
│  │  │  └─ Game/                 GameSession(전체 흐름 Facade), GameConfig
│  │  └─ Unity/                   ← Unity 어댑터 (Darkest.Unity.asmdef)
│  │     ├─ Data/                 ScriptableObject (Skill/HeroClass/Monster/Group/Curio/Resolve/ContentDatabase/BalanceConfig)
│  │     ├─ Infrastructure/       UnityGameLogger, FileSaveRepository, PlaceholderSpriteProvider (Adapter)
│  │     ├─ Presentation/         GameBootstrap(Composition Root), SceneScaffold
│  │     │  ├─ Ui/                UiFactory, PooledButtonList(Object Pool), UiTheme
│  │     │  ├─ Views/             Town/Dungeon/Battle/Result View, BattleStageView+UnitView(Pool, Gizmos), BattleAnimationPlayer, EventLogView
│  │     │  └─ Presenters/        Town/Dungeon/Battle/Result Presenter, ScreenRouter, EventLogPresenter, BattleAnimationPresenter
│  │     └─ Debugging/            GameDebugView, ReadOnlyFieldAttribute
│  ├─ Editor/                     콘텐츠 에셋 생성기, 씬 생성, ReadOnly 드로어
│  ├─ Tests/EditMode/             NUnit 테스트 (도메인만 참조)
│  ├─ Resources/Darkest/Placeholders/   더미 초상화 PNG
│  └─ Scenes/Main.unity
└─ Tools/                         Unity 밖 빌드/테스트용 csproj (소스는 Assets에서 링크)
```

**의존 방향**: `Unity(Presentation/Infrastructure/Data) → Domain`. Domain은 UnityEngine을 참조할 수 없다(asmdef로 강제).

---

## 3. 핵심 클래스 다이어그램

```
                        ┌──────────────────────── GameBootstrap (MonoBehaviour, Composition Root) ─────────────────────┐
                        │ 생성/주입: IGameLogger, IRandom(seed), IEventBus, IContentCatalog, GameConfig, SaveGateway   │
                        └───────────────┬───────────────────────────────────────────────┬───────────────────────────────┘
                                        │                                               │
                         ┌──────────────▼──────────────┐                 ┌──────────────▼───────────────┐
                         │ GameSession (Facade)        │  GameModeChanged│ ScreenRouter                  │
                         │  Mode: Town/Exploring/      │ ───────────────►│  Town/Dungeon/Battle/Result   │
                         │        Battle/Result        │   (IEventBus)   │  Presenter.Enter/Exit         │
                         │  TryEmbark / TryMoveTo /    │                 └──────────────┬───────────────┘
                         │  SubmitBattleCommand / ...  │◄── 호출 ──── XxxPresenter ◄─ 이벤트 ─ XxxView (uGUI)
                         └──┬──────────┬──────────┬────┘
               ┌────────────┘          │          └──────────────┐
     ┌─────────▼────────┐   ┌──────────▼─────────┐   ┌───────────▼───────────┐
     │ TownService      │   │ Expedition         │   │ BattleSession         │
     │  Town(roster,    │   │  DungeonMap, Light │   │  BattleContext(상태)   │
     │  gold, recruits) │   │  Inventory         │   │  BattleState (State)  │
     └────────┬─────────┘   │  CorridorTravel    │   │   RoundStart→TurnStart│
              │             │  CurioResolver     │   │   →AwaitingHero|Enemy │
              │             └─────────┬──────────┘   │   →CheckEnd→Victory.. │
              │                       │              └───────────┬───────────┘
              │                       │                          │ IBattleCommand (Command)
              │                       │                ┌─────────▼──────────┐
              │                       │                │ UseSkillCommand    │──► IHitResolver / IDamageCalculator (Strategy)
              │                       │                │ MoveCommand, Pass  │──► ISkillEffect[] (Strategy: Bleed/Stun/Heal/Stress/Move)
              │                       │                └────────────────────┘◄── IEnemyAI (Strategy)
              └───────────┬───────────┴───────────────────────────┘
                 ┌────────▼──────────┐       Combatant ◄─ Hero (StressMeter, Death's Door)
                 │ StressService     │                 ◄─ Monster
                 │ 결의 판정/심장마비 │       Formation (1~4열, 이동/사망 정리)
                 └───────────────────┘
  CombatantFactory(Definition → 인스턴스)   IContentCatalog ◄─ ContentCatalog ◄─ DefaultContent | ContentDatabaseSO
  SaveGateway = ISaveRepository(FileSaveRepository) + ISaveSerializer(LineSaveSerializer) + SaveMapper
```

### 적용 패턴 요약
| 상황 | 패턴 | 위치 |
|---|---|---|
| 전투 단계마다 행동이 다름 | **State** | `BattleStates.cs` (상태 객체는 무상태 → 1회 생성 재사용) |
| 행동 실행/기록/재현 | **Command** | `IBattleCommand`, `BattleContext.History` |
| 명중·피해·턴 순서·AI·스킬 효과 교체 | **Strategy** | `IHitResolver`, `IDamageCalculator`, `ITurnOrderStrategy`, `IEnemyAI`, `ISkillEffect` |
| 도메인 → UI 알림 | **Observer/EventBus** | `IEventBus`, `*Events.cs` |
| 데이터 → 런타임 객체 | **Factory** | `CombatantFactory`, `SkillEffectFactory` |
| 반복 생성되는 UI/유닛 | **Object Pool** | `PooledButtonList`, `BattleStageView` |
| UI ↔ 게임플레이 | **MVP** | `*View`(그리기/입력) ↔ `*Presenter`(조율) ↔ `GameSession` |
| 외부 시스템(로그/파일/리소스) | **Adapter** | `UnityGameLogger`, `FileSaveRepository`, `PlaceholderSpriteProvider` |
| 복잡한 하위 시스템 묶기 | **Facade** | `GameSession`, `BattleSession` |

---

## 4. 사용 예시

### 씬 구성
| GameObject | 컴포넌트 | 값 |
|---|---|---|
| `GameBootstrap` | `GameBootstrap` | Content/Balance: 비움(더미) 또는 생성한 SO, Use Random Seed: 체크, Log Level: Info |
| (자동 생성) `Main Camera` | Camera | Orthographic, size 5 |
| (자동 생성) `BattleStage` | `BattleStageView` | 런타임 생성 |
| (자동 생성) `DarkestCanvas` | Canvas + 각 View | 런타임 생성 |

### 새 스킬 추가 (데이터만)
1. `Create → Darkest → Skill` → `Usable From` = `1,2`, `Target Ranks` = `1,2,3`, `Damage Percent` = 80
2. `Effects`에 `Bleed / amount 3 / duration 3 / chance 100` 추가
3. 영웅 직업 SO의 `Skills` 목록에 넣는다. 코드 수정 없음.

### 코드에서 전투만 돌리기 (테스트/툴)
```csharp
var random = new SeededRandom(42);
var events = new EventBus();
var catalog = DefaultContent.Create();
var stress = new StressService(new StressRules(), catalog.Afflictions, catalog.Virtues, random, events, NullLogger.Instance);
var services = new BattleServices(random, events, NullLogger.Instance, new CombatRules(), stress);

var battle = new BattleSession(heroFormation, monsterFormation, services);
events.Subscribe<DamageDealt>(e => Console.WriteLine(e));
battle.Start();                                     // 첫 영웅 차례까지 자동 진행
battle.Submit(new UseSkillCommand(battle.CurrentActor, skill, target)); // 실패하면 사유 문자열 반환
```

---

## 5. 확장 포인트

| 늘리고 싶은 것 | 어디를 | 비고 |
|---|---|---|
| 새 스킬 효과(표식, 버프, 회복 차단…) | `ISkillEffect` 구현 + `SkillEffectKind`/`SkillEffectFactory`에 한 줄 | `UseSkillCommand` 수정 불필요 |
| 빛 보정 수치/규칙 | `ExpeditionRules.*ByLight` 배열(BalanceConfig SO에서 편집), 규칙 자체는 `LightEffects` | 몬스터에 출처 "light" StatModifier로 붙음 |
| 보스 AI, 위협도 기반 AI | `IEnemyAI` 구현. 몬스터 정의에 AI 키를 두고 팩토리로 선택 | |
| 새 상태이상 | `StatusKind` + `StatusEffectSet` 처리 + 효과 클래스 | |
| 야영/함정/복도 조우 | `CorridorTravel.Step`, `RoomType` + `Expedition.EnterRoom` | |
| 마을 건물 | `TownService`에 서비스 메서드 or 건물별 서비스 클래스 분리 | |
| 연출(애니메이션/이펙트) | `BattleAnimationPresenter`(이벤트→비트) + `BattleAnimationPlayer`(재생). 새 이벤트 연출은 Presenter에 한 줄 | 전투가 끝나도 `ContinueAfterBattle` 전까지 전투 화면 유지 → 마지막 일격까지 재생 후 결과 패널 |
| 프리팹 UI | `SceneScaffold`/`UiFactory`만 교체. View의 이벤트/Render API 유지 | |
| 세이브 포맷(JSON 등) | `ISaveSerializer` 새 구현. 탐험 저장은 `ExpeditionSaveData` + `ExpeditionSaveMapper` | 상태이상·RNG 상태 저장은 아직 없음 |
| 성장(레벨 보상) | `ProgressionRules`(경험치 표·레벨당 보정) + `HeroProgression` | 수치는 `BalanceConfigSO` Progression 섹션 |
| 리플레이 | seed + `BattleContext.History`(명령 기록) 재실행 | 테스트 `SameSeed_ReplaysIdentically` 참고 |

전투 시작/종료/도주 연결부는 `ExpeditionBattles`로 분리했다. `GameSession`(≈230줄)이 더 커지면 저장/불러오기 연결부를 다음 분리 후보로 둔다.

---

## 6. 예상되는 버그 / 주의점

1. **입력 시스템 설정 불일치**: Input System 패키지 설치 여부로 입력 모듈을 고른다. 패키지는 설치돼 있는데
   `Player → Active Input Handling`이 *Input Manager (Old)* 전용이면(또는 반대) 클릭이 안 된다 → **Both**로 두면 안전하다.
2. **도메인 객체 공유 상태**: 탐험 파티 `Formation`은 전투에도 그대로 쓰인다(위치/사망 정리가 이어지도록 의도).
   전투 중 사망자는 진형에서 즉시 빠지므로, 사망자 집계는 진형이 아니라 `Expedition.Embarked/Fallen` 기준으로 해야 한다.
   또 `SkillDefinition`은 여러 영웅이 공유하는 불변 데이터이므로 런타임 상태를 넣으면 안 된다.
3. **이벤트 재진입과 화면 전환 타이밍**: `SubmitBattleCommand` 한 번에 몬스터 턴들이 연속 실행된다. 전투가 끝나면
   세션은 `Battle` 모드에 머물고(`IsBattleConcluded`, `LastBattleSummary`), 결과 패널의 "계속"이 `ContinueAfterBattle`을
   불러야 탐험/결과 화면으로 넘어간다. 탐험 코드나 봇이 전투 뒤 바로 다음 행동을 하려면 이 호출을 잊지 말 것.
   (구독 해제는 `Dispose` — 새 게임 시 `ScreenRouter`가 일괄 해제)

기타: 한글/기호(▶ ✝ ★) 표시는 OS 폰트 폴백에 의존한다(내장 Legacy 폰트). 깨지면 TMP 한글 폰트로 교체 권장.
`.meta` 파일은 `GameBootstrap.cs`와 `Main.unity`만 동봉했다(씬 참조 유지용). 나머지는 Unity가 처음 열 때 생성하므로 그대로 커밋하면 된다.
