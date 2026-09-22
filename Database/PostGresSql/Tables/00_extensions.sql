/*
    Enables the citext extension, used by the Users table so that
    username/email lookups and uniqueness are case-insensitive without every
    query needing an explicit LOWER(...) - matching the app's use of
    StringComparison.OrdinalIgnoreCase in IUserRepository.
*/

CREATE EXTENSION IF NOT EXISTS citext;
