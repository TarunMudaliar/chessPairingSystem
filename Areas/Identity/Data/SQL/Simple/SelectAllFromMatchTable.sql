SELECT
  GameId,
  WhitePlayerId,
  BlackPlayerId,
  CONVERT(varchar(19), MatchDate, 120) AS MatchDate,
  Location,
  ScheduledTime
FROM Match;