namespace Tessera.Games.Yacht
{
    /// <summary>
    /// 족보 칸의 영문 표기입니다. 점수표 칸 이름과 퀘스트 진행도 문구가 같은 표기를 쓰도록
    /// 이 테이블 하나로 모읍니다.
    /// </summary>
    public static class ScoreCategoryNames
    {
        public static string Get(ScoreCategory category) => category switch
        {
            ScoreCategory.Aces => "Aces",
            ScoreCategory.Deuces => "Deuces",
            ScoreCategory.Threes => "Threes",
            ScoreCategory.Fours => "Fours",
            ScoreCategory.Fives => "Fives",
            ScoreCategory.Sixes => "Sixes",
            ScoreCategory.Choice => "Choice",
            ScoreCategory.FourOfAKind => "4 of a Kind",
            ScoreCategory.FullHouse => "Full House",
            ScoreCategory.SmallStraight => "S. Straight",
            ScoreCategory.LargeStraight => "L. Straight",
            ScoreCategory.Yacht => "Yacht",
            _ => ""
        };
    }
}
