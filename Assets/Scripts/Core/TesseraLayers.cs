namespace Tessera.Core
{
    /// <summary>
    /// 프로젝트에서 쓰는 레이어 번호. ProjectSettings/TagManager.asset 과 일치해야 한다.
    ///
    /// 기존 코드는 <c>DecorationLayer = 11</c> 같은 상수를 파일마다 따로 선언하고 있다.
    /// 신규 코드는 이 클래스를 쓰고, 기존 중복 선언 정리는 M10 프레젠테이션 리팩토링에서 함께 한다.
    /// </summary>
    public static class TesseraLayers
    {
        public const int Default = 0;

        /// <summary>주사위 본체. 전용 조명과 그림자 처리를 받는다.</summary>
        public const int Dice = 8;

        public const int TrayFloor = 9;
        public const int TrayWall = 10;

        /// <summary>테이블 위 장식 프롭. 픽셀 필터를 그대로 통과한다.</summary>
        public const int Decoration = 11;

        /// <summary>
        /// 픽셀 필터를 거치지 않고 원본 해상도로 그려야 하는 월드 스페이스 UI.
        /// 월드 카메라의 컬링 마스크에서 제외되고, 전용 Crisp UI 카메라만 이 레이어를 찍는다.
        /// </summary>
        public const int CrispUI = 16;

        public static int Mask(int layer) => 1 << layer;
    }
}
