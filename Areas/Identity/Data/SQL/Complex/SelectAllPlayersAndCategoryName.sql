SELECT u.PlayerName, 
       u.Email, 
       u.Ratings,
       c.CategoryName
FROM AspNetUsers u
JOIN Category c ON u.CategoryId = c.CategoryId;