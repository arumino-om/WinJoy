using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinJoy.Data
{
    static internal class BodyColorName
    {
        static Dictionary<string, string> mapping = new()
        {
            // Developer Kit Joy-Con
            { "313131", "ブラック" },
                
            // Standard Retail Joy-Con Color
            { "828282", "グレー" },
            { "FF3C28", "ネオンレッド" },
            { "0AB9E6", "ネオンブルー" },
            { "E6FF00", "ネオンイエロー" },
            { "1EDC00", "ネオングリーン" },
            { "FF3278", "ネオンピンク" },
            { "E10F00", "レッド" },
            { "4655F5", "ブルー" },
            { "B400E6", "ネオンパープル" },
            { "FAA005", "ネオンオレンジ" },
            { "E6E6E6", "ホワイト" },
            { "FFAFAF", "パステルピンク" },
            { "F5FF82", "パステルイエロー" },
            { "F0CBEB", "パステルパープル" },
            { "BCFFC8", "パステルグリーン" },

            // Special Edition Joy-Con Color
            { "C88C32", "ポケットモンスター Let's Go! イーブイ" },
            { "FFDC00", "ポケットモンスター Let's Go! ピカチュウ" },
            { "D7AA73", "Nintendo Labo Creators Contest Edition \"Cardboard\"-Colored" },
            { "1473FA", "ドラゴンクエスト XI S （ロト版）" },
            { "82FF96", "あつまれ どうぶつの森" },
            { "96F5F5", "あつまれ どうぶつの森" },
            { "F04614", "マリオ レッド × ブルー" },
            { "818282", "モンスターハンターライズ" },
            { "0084FF", "フォートナイト フリート フォース(力の艦隊)" },
            { "2D50F0", "ゼルダの伝説 スカイウォードソード" },
            { "500FC8", "ゼルダの伝説 スカイウォードソード" },
            { "6455F5", "（有機ELモデル） スプラトゥーン3" },
            { "C3FA05", "（有機ELモデル） スプラトゥーン3" },
            { "F07341", "（有機ELモデル） ポケットモンスター スカーレット・バイオレット" },
            { "9650AA", "（有機ELモデル） ポケットモンスター スカーレット・バイオレット" },
            { "D2BE69", "（有機ELモデル） ゼルダの伝説　ティアーズ オブ ザ キングダム" }
        };

        static public string GetName(string color)
        {
            if (mapping.TryGetValue(color, out string name)) {
                return name;
            }
            return "Unknown";

        }
    }
}
