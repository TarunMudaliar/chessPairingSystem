SELECT m.GameId, 
       wp.PlayerName AS WhitePlayer, 
       bp.PlayerName AS BlackPlayer, 
       m.Status, 
       CONVERT(varchar(19), m.MatchDate, 120) AS MatchDate
FROM Match m
JOIN AspNetUsers wp ON m.WhitePlayerId = wp.Id
JOIN AspNetUsers bp ON m.BlackPlayerId = bp.Id;