SELECT a.AppealId,
       u.UserName AS SubmittedBy,
       a.Message,
       a.Status,
       m.MatchDate
FROM Appeal a
JOIN AspNetUsers u ON a.PlayerId = u.Id
JOIN Match m ON a.GameId = m.GameId
WHERE a.Status = 'Pending';