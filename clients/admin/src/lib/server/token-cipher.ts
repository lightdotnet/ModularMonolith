import "server-only";

import { createCipheriv, createDecipheriv, randomBytes } from "crypto";
import { getTokenEncryptionKey } from "@/lib/server/config";

const ALGORITHM = "aes-256-gcm";
const IV_LENGTH = 12;
const KEY_LENGTH = 32;

function getKey(): Buffer {
  const key = Buffer.from(getTokenEncryptionKey(), "base64");
  if (key.length !== KEY_LENGTH) {
    throw new Error(
      `TOKEN_ENCRYPTION_KEY must decode to ${KEY_LENGTH} bytes (got ${key.length}).`,
    );
  }
  return key;
}

/** Encrypts a plaintext string, returning "iv.authTag.ciphertext" (each base64). */
export function encrypt(plaintext: string): string {
  const iv = randomBytes(IV_LENGTH);
  const cipher = createCipheriv(ALGORITHM, getKey(), iv);
  const ciphertext = Buffer.concat([
    cipher.update(plaintext, "utf8"),
    cipher.final(),
  ]);
  const authTag = cipher.getAuthTag();

  return [iv, authTag, ciphertext].map((buf) => buf.toString("base64")).join(".");
}

/** Decrypts a payload produced by `encrypt`. Returns null on any malformed/tampered input. */
export function decrypt(payload: string): string | null {
  const parts = payload.split(".");
  if (parts.length !== 3) return null;

  try {
    const [iv, authTag, ciphertext] = parts.map((part) => Buffer.from(part, "base64"));
    const decipher = createDecipheriv(ALGORITHM, getKey(), iv);
    decipher.setAuthTag(authTag);
    const plaintext = Buffer.concat([decipher.update(ciphertext), decipher.final()]);
    return plaintext.toString("utf8");
  } catch {
    return null;
  }
}
