# self-dev-ai-pilot

Copilot 기반으로 시작하는 1인 개발 프로젝트입니다. 현재 내 개발 역량과 최신 AI 도구들을 조합하여, 어디까지 구현이 가능한지 실험하는 목적의 저장소입니다.

This is a solo development project started using Copilot, testing how far I can go by combining my own coding skills and modern AI tools.

## JSON 데이터 에디터

이 프로젝트에는 JSON 데이터를 편집할 수 있는 강력한 에디터가 포함되어 있습니다. 이 에디터는 Unity 환경에서 동작하며, 다음과 같은 기능을 제공합니다:

- JSON 파일을 선택하여 데이터를 로드하고 편집할 수 있습니다.
- 카테고리별로 데이터를 관리할 수 있습니다.
- 각 데이터 항목의 속성을 실시간으로 수정할 수 있습니다.
- 검색 기능을 통해 특정 데이터를 빠르게 찾을 수 있습니다.
- 검색된 데이터는 하이라이트 처리되어 가독성을 높입니다.
- 데이터 항목을 추가하거나 삭제할 수 있습니다.
- 수정된 데이터를 JSON 파일로 저장할 수 있습니다.

### 작업 시간 및 기반

- 모든 기능은 Copilot 기반으로 개발되었습니다.
- 작업 시간은 약 5시간이 소요되었습니다.

### 현재 데이터 파일

현재 JSON 데이터는 `c:\Users\{유저명}\AppData\LocalLow\DefaultCompany\ToyMakerPresentation\game_data.json` 경로에 저장되어 있으며, 다음과 같은 구조를 가지고 있습니다:

```json
{
  "CardSpec": {
    "1": {
      "Id": 1,
      "cardName": "SampleCard1",
      "attack": 10,
      "defense": 5
    },
    "6": {
      "Id": 7,
      "cardName": "SampleCard2",
      "attack": 15,
      "defense": 99
    },
    "3": {
      "Id": 3,
      "cardName": "SampleCard3",
      "attack": 20,
      "defense": 12
    },
    "4": {
      "Id": 4,
      "cardName": "SampleCard4",
      "attack": 25,
      "defense": 15
    },
    "5": {
      "Id": 5,
      "cardName": "SsmepleCard5",
      "attack": 35,
      "defense": 20
    }
  }
}
```

이 데이터는 `AppData` 경로에 저장된 것으로 가정하고 작업하고 있습니다.
