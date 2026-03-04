using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace DbCore
{
    public class DatabaseManager
    {
        private readonly string _connectionString;

        public DatabaseManager(string dbPath = "stemd.db")
        {
            _connectionString = $"Data Source={dbPath}";
        }

        public void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL,
                    TotalScore INTEGER DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS Subjects (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Progress (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    SubjectId INTEGER NOT NULL,
                    IsCompleted INTEGER DEFAULT 0,
                    FOREIGN KEY(UserId) REFERENCES Users(Id),
                    FOREIGN KEY(SubjectId) REFERENCES Subjects(Id)
                );
            ";
            command.ExecuteNonQuery();

            // Seed Some Data if extremely empty
            SeedData(connection);
        }

        private void SeedData(SqliteConnection connection)
        {
            var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT COUNT(*) FROM Subjects;";
            long count = (long)checkCommand.ExecuteScalar();

            if (count == 0)
            {
                var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = @"
                    INSERT INTO Subjects (Name) VALUES ('Physics - Kinematics');
                    INSERT INTO Subjects (Name) VALUES ('Chemistry - Reactions');
                    INSERT INTO Subjects (Name) VALUES ('Computer Science - AI Basics');
                ";
                insertCommand.ExecuteNonQuery();
            }
        }

        public void AddUser(string username)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Users (Username, TotalScore) VALUES ($username, 0)";
            command.Parameters.AddWithValue("$username", username);
            command.ExecuteNonQuery();
        }

        public List<User> GetAllUsers()
        {
            var users = new List<User>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Username, TotalScore FROM Users";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    TotalScore = reader.GetInt32(2)
                });
            }

            return users;
        }

        public void UpdateProgress(int userId, int subjectId, bool completed, int scoreGain)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();
            
            try
            {
                // Insert or update progress
                var progCmd = connection.CreateCommand();
                progCmd.CommandText = @"
                    INSERT INTO Progress (UserId, SubjectId, IsCompleted) 
                    VALUES ($userId, $subjectId, $completed);
                ";
                progCmd.Parameters.AddWithValue("$userId", userId);
                progCmd.Parameters.AddWithValue("$subjectId", subjectId);
                progCmd.Parameters.AddWithValue("$completed", completed ? 1 : 0);
                progCmd.ExecuteNonQuery();

                // Update User Score
                var scoreCmd = connection.CreateCommand();
                scoreCmd.CommandText = @"
                    UPDATE Users SET TotalScore = TotalScore + $scoreGain WHERE Id = $userId;
                ";
                scoreCmd.Parameters.AddWithValue("$scoreGain", scoreGain);
                scoreCmd.Parameters.AddWithValue("$userId", userId);
                scoreCmd.ExecuteNonQuery();

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
