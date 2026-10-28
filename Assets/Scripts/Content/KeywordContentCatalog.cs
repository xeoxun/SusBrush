using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public enum LiarPainterKeywordCategory
{
    Animal,
    SeaCreature,
    Food,
    DessertAndFruit,
    Household,
    Electronics,
    Vehicle,
    Nature,
    Fantasy,
    PlayAndSports,
    Animation
}

[Serializable]
public sealed class LiarPainterKeywordEntry
{
    public string keyword;
    public LiarPainterKeywordCategory category;

    public LiarPainterKeywordEntry(string value, LiarPainterKeywordCategory keywordCategory)
    {
        keyword = value;
        category = keywordCategory;
    }
}

[Serializable]
public sealed class LiarPainterCategoryQuestions
{
    public LiarPainterKeywordCategory category;
    public List<string> questions = new();
}

[CreateAssetMenu(fileName = "KeywordContentCatalog", menuName = "SusBrush/Keyword Content Catalog")]
public sealed class KeywordContentCatalog : ScriptableObject
{
    [SerializeField] private List<string> questions = new();
    [SerializeField] private List<LiarPainterKeywordEntry> keywords = new();
    [SerializeField] private List<LiarPainterCategoryQuestions> categoryQuestions = new();

    public IReadOnlyList<string> Questions => questions;
    public IReadOnlyList<LiarPainterKeywordEntry> Keywords => keywords;
    public IReadOnlyList<LiarPainterCategoryQuestions> CategoryQuestions => categoryQuestions;

    public IReadOnlyList<string> GetCategoryQuestions(LiarPainterKeywordCategory category) => categoryQuestions
        .Where(group => group != null && group.category == category)
        .SelectMany(group => group.questions ?? new List<string>())
        .Where(question => !string.IsNullOrWhiteSpace(question)).Distinct().ToArray();

    public void ReplaceContent(IEnumerable<string> newQuestions, IEnumerable<LiarPainterKeywordEntry> newKeywords)
    {
        questions = newQuestions?.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToList() ?? new List<string>();
        keywords = newKeywords?.Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.keyword)).ToList()
                   ?? new List<LiarPainterKeywordEntry>();
    }

    public readonly struct WordPair
    {
        public readonly string commonWord, differentWord, category;
        public WordPair(string common, string different, string categoryName)
        { commonWord = common; differentWord = different; category = categoryName; }
    }

    private static readonly string[] CategoryNames =
    {
        "동물", "바다 생물", "음식", "디저트·과일", "생활용품", "전자기기",
        "탈것", "자연", "판타지", "놀이·스포츠", "애니메이션"
    };
    private static string Clean(string value) => (value ?? "").Normalize(NormalizationForm.FormKC).Trim();

    private string[] GetQuestionPool(LiarPainterKeywordCategory category) =>
        questions.Concat(GetCategoryQuestions(category)).Where(q => !string.IsNullOrWhiteSpace(q))
            .Select(Clean).Distinct().ToArray();

    public bool TryValidate(out string error)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (keywords == null || keywords.Count == 0)
        { error = "카탈로그에 제시어가 없습니다."; return false; }
        foreach (var entry in keywords)
        {
            if (entry == null || !Enum.IsDefined(typeof(LiarPainterKeywordCategory), entry.category))
            { error = "카탈로그에 올바르지 않은 제시어 분류가 있습니다."; return false; }
            var word = Clean(entry.keyword);
            if (word.Length == 0 || word.Length > 32 || Encoding.UTF8.GetByteCount(word) > 125)
            { error = "제시어는 비어 있지 않은 32글자 이하의 단어여야 합니다."; return false; }
            if (!seen.Add(string.Concat(word.Where(c => !char.IsWhiteSpace(c)))))
            { error = "중복 제시어: " + word; return false; }
        }
        foreach (var group in keywords.GroupBy(e => e.category))
        {
            var name = CategoryNames[(int)group.Key];
            if (group.Count() < 2)
            { error = name + ": 서로 다른 제시어가 2개 이상 필요합니다."; return false; }
            var pool = GetQuestionPool(group.Key);
            if (pool.Length < SusBrush.Gameplay.MatchRules.RoundCount)
            { error = name + ": 서로 다른 라운드 질문이 2개 이상 필요합니다."; return false; }
            if (pool.Any(q => Encoding.UTF8.GetByteCount(q) > 509))
            { error = name + ": 질문이 네트워크 전송 길이를 초과합니다."; return false; }
        }
        error = "";
        return true;
    }

    public bool TryDrawWords(out WordPair pair)
    {
        pair = default;
        if (!TryValidate(out _)) return false;
        var common = keywords[UnityEngine.Random.Range(0, keywords.Count)];
        var alternatives = keywords.Where(e => e.category == common.category && e != common).ToArray();
        var different = alternatives[UnityEngine.Random.Range(0, alternatives.Length)];
        pair = new WordPair(Clean(common.keyword), Clean(different.keyword), CategoryNames[(int)common.category]);
        return true;
    }

    public bool TryDrawQuestion(string category, IReadOnlyCollection<string> used, out string question)
    {
        question = "";
        var index = Array.IndexOf(CategoryNames, category);
        if (index < 0) return false;
        var available = GetQuestionPool((LiarPainterKeywordCategory)index)
            .Where(q => used == null || !used.Contains(q)).ToArray();
        if (available.Length == 0) return false;
        question = available[UnityEngine.Random.Range(0, available.Length)];
        return true;
    }

    public bool TryCreateRound(System.Random random, out LiarPainterRoundContent content)
    {
        content = null;
        if (random == null) throw new ArgumentNullException(nameof(random));

        string[] validQuestions = questions.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        LiarPainterKeywordEntry[] validKeywords = keywords
            .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.keyword))
            .ToArray();
        LiarPainterKeywordEntry[] selectableCitizenKeywords = validKeywords
            .Where(entry => validKeywords.Count(other => other.category == entry.category && other.keyword != entry.keyword) > 0)
            .ToArray();

        if (validQuestions.Length == 0 || selectableCitizenKeywords.Length == 0) return false;

        LiarPainterKeywordEntry citizen = selectableCitizenKeywords[random.Next(selectableCitizenKeywords.Length)];
        LiarPainterKeywordEntry[] drunkCandidates = validKeywords
            .Where(entry => entry.category == citizen.category && entry.keyword != citizen.keyword)
            .ToArray();
        LiarPainterKeywordEntry drunk = drunkCandidates[random.Next(drunkCandidates.Length)];

        content = new LiarPainterRoundContent
        {
            citizenKeyword = citizen.keyword,
            drunkPainterKeyword = drunk.keyword,
            question = validQuestions[random.Next(validQuestions.Length)]
        };
        return true;
    }
}

public static class KeywordContentProvider
{
    private const string ResourceName = "KeywordContentCatalog";

    public static KeywordContentCatalog LoadDefault()
    {
        return Resources.Load<KeywordContentCatalog>(ResourceName);
    }
}
