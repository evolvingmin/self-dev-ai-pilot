# self-dev-ai-pilot

이 프로젝트는 최신 AI 기술을 활용하여 게임 프로토타입을 개발하는 과정을 탐구하는 실험적 시도입니다. Copilot과 같은 AI 도구를 통해 개발 속도를 높이고, 개인의 역량을 극대화하여 창의적인 아이디어를 구현하는 데 초점을 맞추고 있습니다. 이를 통해 현재 나의 개발 능력을 객관적으로 평가하고, AI와 협업하여 어디까지 발전할 수 있는지 탐구하는 것이 목표입니다.

This project is an experimental journey into developing a game prototype using cutting-edge AI tools like Copilot. The focus is on accelerating development, maximizing individual capabilities, and bringing creative ideas to life. Through this, the goal is to objectively assess my current development skills and explore how far I can go by collaborating with AI.

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

## 앞으로의 계획

이 프로젝트는 프로토타이핑 게임을 만들면서 필요한 유틸리티나 구성을 만들고 보완하는 과정을 거칠 것입니다. 이를 통해 게임 개발의 다양한 측면을 탐구하고, AI 도구를 활용하여 효율성을 극대화하는 방법을 실험할 것입니다.
