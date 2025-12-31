UPDATE Users 
SET Role = 'Admin' 
WHERE Username = 'mitesh';

SELECT Id, Username, Role FROM Users WHERE Username = 'mitesh';
