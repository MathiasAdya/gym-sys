namespace GymSys.Core.Entities;

// Disimpan sebagai teks di database (lihat CHECK constraint di PRD Bagian 10).
public enum UserRole { Admin, Resepsionis, Instruktur }

public enum MetodeBayar { Tunai, Transfer }

public enum StatusTransaksi { Lunas, Batal }
