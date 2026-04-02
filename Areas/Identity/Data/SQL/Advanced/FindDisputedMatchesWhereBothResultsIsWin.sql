SELECT m.GameId,
       wp.PlayerName AS WhitePlayer,
       bp.PlayerName AS BlackPlayer,
       m.WhiteResult,
       m.BlackResult,
       m.MatchDate
FROM Match m
JOIN AspNetUsers wp ON m.WhitePlayerId = wp.Id
JOIN AspNetUsers bp ON m.BlackPlayerId = bp.Id
WHERE m.WhiteResult = 'W' AND m.BlackResult = 'W';