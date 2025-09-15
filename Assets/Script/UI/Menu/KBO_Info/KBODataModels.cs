/*
 * KBO 리그 데이터 모델 클래스들
 * 
 * API 정보:
 * 1. 타자 API (/api/hitters/top) - 상위 10명의 타자 정보
 * 2. 투수 API (/api/pitchers/top) - 상위 10명의 투수 정보  
 * 3. 팀 순위 API (/api/team-rankings) - KBO 팀 순위 정보
 * 
 * 서버 URL: http://15.164.96.52:8080
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace KBO.Data
{
    /// <summary>
    /// 타자 정보 데이터 클래스
    /// API: /api/hitters/top
    /// </summary>
    [System.Serializable]
    public class HitterData
    {
        [System.Serializable]
        public class PlayerID
        {
            public string playerName;  // 선수 이름
            public string team;        // 소속 팀
            public int year;          // 시즌 연도
        }

        public PlayerID id;
        public float avg;       // 타율 (안타를 타수로 나눈 값)
        public int g;           // 경기 수
        public int pa;          // 타석 (타자가 타석에 들어선 횟수)
        public int ab;          // 타수 (희생번트나 희생플라이를 제외한 타격 횟수)
        public int r;           // 득점
        public int h;           // 안타
        public int doubles;     // 2루타
        public int triples;     // 3루타
        public int hr;          // 홈런
        public int tb;          // 총 루타 (안타, 2루타, 3루타, 홈런으로 얻은 총 베이스 수)
        public int rbi;         // 타점 (타자가 친 안타 등으로 팀의 득점에 기여한 횟수)
        public int sac;         // 희생번트
        public int sf;          // 희생플라이
    }

    /// <summary>
    /// 투수 정보 데이터 클래스
    /// API: /api/pitchers/top
    /// </summary>
    [System.Serializable]
    public class PitcherData
    {
        [System.Serializable]
        public class PlayerID
        {
            public string playerName;  // 선수 이름
            public string team;        // 소속 팀
            public int year;          // 시즌 연도
        }

        public PlayerID id;
        public float era;       // 평균자책점 (9이닝 동안 내준 비자책점의 평균)
        public float ip;        // 이닝 (투구한 총 이닝 수)
        public int w;           // 승리
        public int l;           // 패배
        public int sv;          // 세이브
        public int so;          // 삼진 (탈삼진)
        public int bb;          // 볼넷
        public int h;           // 피안타 (상대 타자에게 내준 안타)
        public int hr;          // 피홈런 (상대 타자에게 내준 홈런)
    }

    /// <summary>
    /// 팀 순위 정보 데이터 클래스
    /// API: /api/team-rankings
    /// </summary>
    [System.Serializable]
    public class TeamRankingData
    {
        [System.Serializable]
        public class TeamID
        {
            public string team;     // 팀 이름
            public int year;        // 시즌 연도
        }

        public TeamID id;
        public int games;           // 총 경기 수
        public int rank;            // 순위
        public int wins;            // 승리
        public int losses;          // 패배
        public int draws;           // 무승부
        public float pct;           // 승률 (승리한 경기수를 총 경기수로 나눈 값)
        public float gb;            // 게임차 (1위 팀과의 승패 차이)
        public string streak;       // 최근 연승/연패 (예: '2패')
        public string last10;       // 최근 10경기 성적
        public string homeRecord;   // 홈 경기 성적
        public string awayRecord;   // 원정 경기 성적
    }

    /// <summary>
    /// KBO 데이터 타입 열거형
    /// </summary>
    public enum KBODataType
    {
        TeamRanking,    // 팀 순위 (기본값)
        Hitters,        // 타자
        Pitchers        // 투수
    }

    /// <summary>
    /// API 응답을 감싸는 래퍼 클래스들
    /// </summary>
    [System.Serializable]
    public class HitterDataList
    {
        public List<HitterData> hitters = new List<HitterData>();
    }

    [System.Serializable]
    public class PitcherDataList
    {
        public List<PitcherData> pitchers = new List<PitcherData>();
    }

    [System.Serializable]
    public class TeamRankingDataList
    {
        public List<TeamRankingData> teams = new List<TeamRankingData>();
    }
}