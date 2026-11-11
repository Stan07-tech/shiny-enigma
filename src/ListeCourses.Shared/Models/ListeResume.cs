namespace ListeCourses.Models;

// Ce que les clients voient d'une liste : un "record" est une classe
// immuable, idéale pour transporter des données (DTO).
public record ListeResume(string Code, string Nom, int Restants);
