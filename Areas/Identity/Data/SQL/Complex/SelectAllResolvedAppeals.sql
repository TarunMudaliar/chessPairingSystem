SELECT a.AppealId,
       u.UserName AS SubmittedBy,
       a.Message,
       a.AdminResponse,
       a.SubmittedAt
FROM Appeal a
JOIN AspNetUsers u ON a.PlayerId = u.Id
WHERE a.Status = 'Resolved'
