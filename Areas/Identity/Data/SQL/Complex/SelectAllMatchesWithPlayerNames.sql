SELECT m.GameId, 
       wp.UserName AS WhitePlayer, 
       bp.UserName AS BlackPlayer, 
       m.Status, 
       m.MatchDate
FROM Match m
JOIN AspNetUsers wp ON m.WhitePlayerId = wp.Id
JOIN AspNetUsers bp ON m.BlackPlayerId = bp.Id;