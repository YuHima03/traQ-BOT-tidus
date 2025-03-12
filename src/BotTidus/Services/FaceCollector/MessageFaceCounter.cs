using BotTidus.Helpers;

namespace BotTidus.Services.FaceCollector
{
    static class MessageFaceCounter
    {
        public static (int PoitiveCount, int NegativeCount) Count(ReadOnlySpan<char> traqMessageText, int currentTotalCount)
        {
            int posCnt = 0;
            int negCnt = 0;

            foreach (var e in new Traq.Extensions.Messages.MessageElementEnumerator(traqMessageText))
            {
                if (e.Kind == Traq.Extensions.Messages.MessageElementKind.NormalText)
                {
                    var t = e.GetText();
                    int i = 0;
                    int cnt;
                    while (i < t.Length)
                    {
                        switch (cnt = countFaceSingle(t[i..], currentTotalCount, out var charsUsed))
                        {
                            case < 0:
                                negCnt -= cnt;
                                break;
                            case > 0:
                                posCnt += cnt;
                                break;
                        }
                        if (charsUsed == 0)
                        {
                            throw new Exception($"`charUsed` is zero. (msg: {t[i..]})");
                        }
                        currentTotalCount += cnt;
                        i += charsUsed;
                    }
                }
            }
            return (posCnt, negCnt);

            static int countFaceSingle(ReadOnlySpan<char> str, int currentTotalCount, out int charsUsed)
            {
                str = str.TrimStart(c => c != '顔', out var trimmedLeading);
                charsUsed = trimmedLeading;
                if (str.IsEmpty)
                {
                    return 0;
                }

                // Now, it is ensured that str is not empty and str[0] is '顔'.
                if (str.Length < 3)
                {
                    charsUsed += str.Length;
                    return 0;
                }

                bool hasParticle_GA;
                if (hasParticle_GA = (str[1] == 'が'))
                {
                    str = str[2..]; // Trim leading "顔が".
                    charsUsed += 2;
                }
                else
                {
                    str = str[1..]; // Trim leading "顔".
                    charsUsed += 1;
                }

                int result = 0;

                if (str.Length < 2)
                {
                    goto returnResult;
                }
                else
                {
                    var sliced = str[..2];
                    if (!hasParticle_GA)
                    {
                        if (sliced is "++")
                        {
                            charsUsed += 2;
                            result = 1;
                            goto returnResult;
                        }
                        else if (sliced is "--")
                        {
                            charsUsed += 2;
                            result = -1;
                            goto returnResult;
                        }
                    }

                    if (sliced is "ある" or "有る" or "誕生" or "爆誕" or "来た" or "きた" or "キタ" or "きちゃ" or "再生" or "開始" or "開花" or "開幕")
                    {
                        charsUsed += 2;
                        result = 1;
                        goto returnResult;
                    }
                    else if (sliced is "ない" or "無い" or "爆発" or "爆散" or "消滅" or "消失" or "終了" or "死亡" or "滅亡" or "蒸発")
                    {
                        charsUsed += 2;
                        result = -1;
                        goto returnResult;
                    }
                }

                if (str.Length < 3)
                {
                    goto returnResult;
                }
                else
                {
                    var sliced = str[..3];
                    if (sliced is "生えた" or "増えた")
                    {
                        charsUsed += 3;
                        result = 1;
                        goto returnResult;
                    }
                    else if (sliced is "終わり" or "消えた" or "滅んだ" or "爆ぜた" or "潰れた")
                    {
                        charsUsed += 3;
                        result = -1;
                        goto returnResult;
                    }
                    else if (sliced is "七面鳥")
                    {
                        charsUsed += 3;
                        result = 7;
                        goto returnResult;
                    }
                }

                if (str.Length < 4)
                {
                    goto returnResult;
                }
                else
                {
                    var sliced = str[..4];
                    if (sliced is "始まった")
                    {
                        charsUsed += 4;
                        result = 1;
                        goto returnResult;
                    }
                    else if (sliced is "終わった")
                    {
                        charsUsed += 4;
                        result = -1;
                        goto returnResult;
                    }
                    else if (sliced is "null")
                    {
                        charsUsed += 4;
                        result = (currentTotalCount > 0) ? -currentTotalCount : -1; // set total count to 0 if positive
                        goto returnResult;
                    }
                }

                if (str.Length < 5)
                {
                    goto returnResult;
                }
                else
                {
                    var sliced = str[..5];
                    if (sliced == "ケルベロス")
                    {
                        charsUsed += 5;
                        result = 3;
                        goto returnResult;
                    }
                    else if (((sliced[0] is 'な' or '無' or '亡') && sliced[1..5] is "くなった")
                        || (sliced[..2] is "吹き" or "吹っ" or "消し" && sliced[2] is 'と' or '飛' && sliced[3..5] is "んだ"))
                    {
                        charsUsed += 5;
                        result = -1;
                        goto returnResult;
                    }
                }

                if (str.Length < 6)
                {
                    goto returnResult;
                }
                else
                {
                    if (str[..6] is ":null:")
                    {
                        charsUsed += 6;
                        result = (currentTotalCount > 0) ? -currentTotalCount : -1; // set total count to 0 if positive
                        goto returnResult;
                    }
                }

                if (str.Length < 7)
                {
                    goto returnResult;
                }
                else
                {
                    if (str[..7] is "ヤマタノオロチ")
                    {
                        charsUsed += 7;
                        result = 8;
                        goto returnResult;
                    }
                }

                if (str.Length < 16)
                {
                    goto returnResult;
                }
                else
                {
                    if (str[..16] is ":trasta_general:")
                    {
                        charsUsed += 16;
                        result = 1;
                        goto returnResult;
                    }
                }

            returnResult:
                return result;
            }
        }
    }
}
