기본적인 유니티 폴더 구성 방식입니다.

ⓐ Assets 폴더에는 각 폴더를 제외한 다른 에셋등을 배치하지 않는걸 기본 원칙으로 한다.

ⓑ 각 폴더의 하위폴더는 기본적으로 Common과 각 Scene별로 나눈다.
   ⓑ - 1. Scene을 하나만 사용할 경우 Scene으로 폴더를 나누지 않아도 되나, 되도록이면 추가 개발의 가능성을 염두에 두고 폴더링을 한다.
   ⓑ - 2. _TirdParty 폴더는 예외적으로 각 에셋의 명칭을 기본으로 한다.

1. _Scripts는 ⓑ를 기반으로, 자체적으로 작성한 스크립트를 둔다.

2. _TirdParty 폴더는 외부 에셋을 담는 폴더이다.

3. Resources 폴더는 ⓑ를 기반으로 하며, 각 폴더마다, Materials, Textures, Prefabs, UIs를 둔다.
   3 - 1. Materials 폴더에는 Shaders 폴더를 추가한다.
   3 - 2. 각 하위 폴더의 리소스 들은 해당 목적에 맞게 다시 폴더를 만들어 정리한다.
	ex) 팝업 리소스의 경우: Popup_Object(목적 명) -> Button_AA .... ZZ, Background, 



※ Resources, StreamingAssets, Fonts, Plugins는 유니티 예약 폴더이므로 명칭을 변경하지 않습니다.
