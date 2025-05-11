# ToyMakerPresentation 개발 가이드: 다음 단계와 포트폴리오 전략

이 문서는 본 프로젝트(카드게임 + 미연시 DataManagerEditor 툴)의 개발 방향, 추천 작업 순서를 위한 실질적 가이드를 제공합니다. 

---

## 1. 프로젝트 요약
- **목표**: 카드게임 및 미연시(비주얼노벨) 데이터 관리/편집을 위한 강력한 Unity Editor 툴(DataManagerEditor) 구축
- **주요 기능**: JSON 기반 데이터 CRUD, 타입/네임스페이스 관리, 카드형 UI, 오브젝트 풀, 현대적 UX, 유지보수성 높은 구조
- **기술 스택**: Unity, Odin Inspector, UniTask, Addressables 등

---

## 2. 추천 작업 순서 (Step by Step)

### 1) 문서화 및 정리
- 본 가이드와 별도로, 각 주요 기능별로 `docs/` 폴더에 간단한 사용법/구조 설명을 추가
- 예시: `docs/DataManagerEditor.md`, `docs/GameObjectPoolManager.md`

### 2) DataManagerEditor 툴 고도화
- **UI/UX 개선**: 카드형 그리드, 섹션 구분, 검색/필터, 즉시 삭제/저장 등
- **코드 분리**: Editor/Runtime, TypeResolver, EditorState 등 역할별로 분리
- **Odin Inspector**: 속성/시리얼라이즈 문제 없는지 점검, 필요시 Odin 기반 Inspector도 유지
- **테스트 데이터**: 실제 카드/보드/캐릭터 데이터로 CRUD 시나리오 반복 테스트

### 3) 데이터 구조/아키텍처 개선
- **TypeResolver**: 런타임/에디터 모두에서 동작하도록 유지
- **DataManager**: 경로 처리, JSON 직렬화, 타입별 데이터 분리 등 견고하게
- **GameObjectPoolManager**: 프리팹 기반, string 키, 확장성/재사용성 점검

### 4) 패키지 관리 및 비동기 처리
- **manifest.json**: Addressables, UniTask(OpenUPM), Odin 등 최신 패키지 반영
- **Async/await**: UniTask 등 활용, 에디터/런타임에서 안전하게 비동기 처리

### 5) 포트폴리오/리크루터 어필 요소 강화
- **README.md**: 프로젝트 목표, 주요 기능, 스크린샷, 차별점, 사용법 요약
- **코드 품질**: 주석, 네이밍, 분리, SOLID 원칙 등 신경쓰기
- **실제 사용 예시**: 카드 추가/삭제/수정, 풀 매니저 활용 등 짧은 GIF/캡처
- **문서화**: docs/ 폴더에 각 기능별 설명, 사용법, 구조도 등

---

## 3. 작업/지시 방식
- **이 가이드에 따라 우선순위/단계별로 작업**
- 막히거나 고민되는 부분은 Copilot에게 구체적으로 질문(예: "풀 매니저에 오브젝트 반환시 자동 비활성화 추가해줘")
- 기능 추가/수정/리팩터링 시, 항상 문서/주석/테스트 데이터도 함께 관리
- 포트폴리오용 스크린샷, GIF, 설명은 작업 중간중간 미리 저장

---

## 4. 추천 폴더 구조
```
Assets/
  Scripts/
    Data/           # 데이터 모델, TypeResolver 등
    ToyProject/     # DataManager 등
    Utils/          # GameObjectPoolManager 등
  Editor/
    DataManagerEditor.cs
  Docs/
    DataManagerEditor.md
    GameObjectPoolManager.md
    ...
README.md
```

---

## 5. Copilot에게 요청 예시
- "카드 데이터에 희귀도 enum 필드 추가하고, 에디터에서 드롭다운으로 편집되게 해줘"
- "풀 매니저에 오브젝트 반환시 자동 비활성화 추가해줘"
- "DataManagerEditor에 다국어 지원(로컬라이제이션) 옵션 추가하고 싶어"
- "포트폴리오용으로 에디터 툴 사용법 GIF 만드는 팁 알려줘"

---

## 6. 마인드셋/스트레스 최소화
- 완벽보다 "작동하는 것" → "정리된 것" → "어필되는 것" 순서로
- 막히면 Copilot에게 바로 질문, 혼자 끙끙대지 않기
- 작은 단위로 자주 저장/커밋, 문서화도 습관처럼

---

이 가이드는 언제든 업데이트/보완 가능. 
