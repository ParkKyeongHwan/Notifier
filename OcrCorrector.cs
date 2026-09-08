namespace Notifier
{
    public class OcrCorrector
    {
        private static readonly List<string> IgnoredKeywords = new List<string>
        {
            // 1. 공통 포함 단어 (이 단어가 들어가면 무조건 스킵)
            "어설트",
            "패러사이트",
            "침공",

            // 2. 고유 어설트 명칭
            "거미의숲",
            "시공",
            "바벨탑",
            "불시착",
            "부산행",
            "나로도",
            "우주센터",
            "우주",
            "마추픽추",
            "마추",
            "픽추",
            "모선",
            "유령선",
            "보르네오섬",
            "보르",
            "피라미드",
            "피라",
            "대마도",
            "명성황후",
            "명성",
            "마피아저택",
            "마피아",
            "하수도",
            "경성역",
            "시티",
            "시티타워",
            "한강둔치",
            "트랜실바니아",
            "트랜실",
            "정글",
            "달기지",
            "연구단지",
        };

        private static readonly Dictionary<string, string> MissionDictionary = new Dictionary<string, string>
        {
            // --- 1. 농업 및 외곽 구역 ---
            { "한계농지", "한계농지 논밭 습격전 (상위 3명)" },
            { "구역방벽", "농업진흥구역 방벽 습격전 (상위 5명)" },
            { "농장폐허", "농업보호구역 농장 폐허 습격전 (상위 15명)" }, // '서쪽산길 폐허'와 충돌 방지
            { "과수원중앙", "과수원 중앙 습격전 (상위 50명)" }, // 다른 '중앙'과 충돌 방지
            { "전면도로", "카페촌 전면도로 습격전 (상위 30명)" },
            { "경작지", "농업진흥구역 경작지 습격전 (상위 30명)" },
            { "팔당댐", "팔당댐 공원 습격전 (상위 20명)" },

            // --- 2. 기업 및 공업 지구 ---
            { "외국기업", "외국기업지구 하부 습격전 (상위 20명)" },
            { "상가단지", "상업지구 상가단지 습격전 (상위 150명)" },
            { "복합지구중앙", "복합지구 중앙 습격전 (상위 10명)" },
            { "복합지구상부", "복합지구 상부공장 습격전 (상위 5명)" },
            { "산란지", "경공업지구 산란지 습격전 (상위 100명)" },
            { "중공업상부", "중공업지구 상부공장 습격전 (상위 20명)" }, 

            // --- 3. 산책로 및 체험농장 ---
            { "산책로광장", "산책로 광장 습격전 (상위 3명)" },
            { "산책로중앙", "산책로 중앙 습격전 (상위 50명)" },
            { "두물경", "산책로 두물경 습격전 (상위 150명)" },
            { "산책로B", "산책로 B (상위 50명)" },
            { "굽은길", "산책로 굽은길 습격전 (상위 15명)" },
            { "체험농장하부", "체험농장 하부 습격전 (상위 30명)" },
            { "체험농장상부", "체험농장 상부 습격전 (상위 10명)" },

            // --- 4. 산길 및 계곡 ---
            { "개미굴", "계곡길 개미굴 습격전 (상위 100명)" },
            { "개미협곡", "계곡길 개미협곡 습격전 (상위 5명)" },
            { "깊은산길중턱", "깊은산길 중턱 습격전 (상위 50명)" },
            { "개미군락", "깊은산길 개미군락 습격전 (상위 3명)" },
            { "운길산초입", "운길산 초입 공터 습격전 (상위 100명)" },
            { "운길산", "운길산 초입 공터 습격전 (상위 100명)" },
            { "초입", "운길산 초입 공터 습격전 (상위 100명)" },
            { "공터", "운길산 초입 공터 습격전 (상위 100명)" },
            { "오솔길중앙", "오솔길 중앙 습격전 (상위 10명)" },
            { "아라크니아", "서쪽산길 아라크니아 습격전 (상위 150명)" },
            { "서쪽산길폐허", "서쪽산길 폐허 습격전 (상위 15명)" },
            { "상위", "알 수 없는 습격전" }
        };

        public static (string Name, string Category) ExtractMissionName(string rawOcrText)
        {
            // 1. OCR의 불필요한 공백과 줄바꿈을 모두 제거
            string cleanText = rawOcrText.Replace(" ", "").Replace("\n", "").Replace("\r", "");

            // 제외 키워드(어설트) 검사
            foreach (var ignored in IgnoredKeywords)
            {
                if (cleanText.Length < ignored.Length) continue;

                for (int i = 0; i <= cleanText.Length - ignored.Length; i++)
                {
                    string window = cleanText.Substring(i, ignored.Length);
                    int allowedErrors = ignored.Length >= 5 ? 2 : 1;

                    if (ComputeLevenshteinDistance(window, ignored) <= allowedErrors)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 어설트 감지 ('{ignored}').");
                        return ($"어설트 ({ignored})", "Assault");
                    }
                }
            }

            string bestMatchName = string.Empty;
            int bestErrorCount = int.MaxValue; // 가장 적은 오타 수 기록

            // 2. 키워드 사전을 순회하며 검사
            foreach (var kvp in MissionDictionary)
            {
                string targetKeyword = kvp.Key;
                string fullName = kvp.Value;

                // 글자가 키워드보다 짧게 인식되었으면 패스
                if (cleanText.Length < targetKeyword.Length) continue;

                // 슬라이딩 윈도우 방식으로 키워드 길이만큼 잘라서 검사
                for (int i = 0; i <= cleanText.Length - targetKeyword.Length; i++)
                {
                    string window = cleanText.Substring(i, targetKeyword.Length);

                    // 두 단어의 차이(오타 개수)를 계산
                    int errorCount = ComputeLevenshteinDistance(window, targetKeyword);

                    // ★ 동적 오타 허용치 적용
                    // 키워드가 3~4글자면 오타 1개 허용, 5글자 이상이면 2개 허용
                    int allowedErrors = targetKeyword.Length >= 5 ? 2 : 1;

                    if (errorCount <= allowedErrors)
                    {
                        // 완벽히 일치(오타 0)하면 무조건 정답이므로 즉시 반환!
                        if (errorCount == 0)
                        {
                            return (fullName, "Success");
                        }

                        if (errorCount < bestErrorCount)
                        {
                            bestErrorCount = errorCount;
                            bestMatchName = fullName;
                        }
                    }
                }
            }

            // 가장 정확도 높은 결과를 찾았다면 반환(예: 1~2글자 오타로 통과한 최고 점수)
            if (!string.IsNullOrEmpty(bestMatchName))
            {
                if (bestMatchName == "알 수 없는 습격전")
                {
                    return (bestMatchName, "Unknown");
                }
                return (bestMatchName, "Success");
            }

            // 구체적인 미션을 못 찾았을 경우 "습격전" 이라는 단어가 있는지 확인
            string fallbackKeyword = "습격";
            if (cleanText.Length >= fallbackKeyword.Length)
            {
                for (int i = 0; i <= cleanText.Length - fallbackKeyword.Length; i++)
                {
                    string window = cleanText.Substring(i, fallbackKeyword.Length);
                    // "습격전"(3글자)이므로 오타 1개까지는 허용하여 융통성 있게 검사
                    if (ComputeLevenshteinDistance(window, fallbackKeyword) <= 1)
                    {
                        return ("알 수 없는 습격전", "Unknown");
                    }
                }
            }

            // 최후의 수단: OCR이 심하게 깨져서 "습" 또는 "격" 단 한 글자만 인식된 경우
            if (cleanText.Contains("습") || cleanText.Contains("격"))
            {
                return ("알 수 없는 습격전", "Unknown");
            }

            return ("미발견", "None");
        }

        private static int ComputeLevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }
    }
}
