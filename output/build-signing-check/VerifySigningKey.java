import java.io.BufferedReader;
import java.io.File;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.security.KeyStore;
import java.util.Arrays;
import java.util.Base64;

class VerifySigningKey {
    public static void main(String[] args) throws Exception {
        BufferedReader reader = new BufferedReader(new InputStreamReader(System.in, StandardCharsets.UTF_8));
        char[] storePassword = new String(Base64.getDecoder().decode(reader.readLine()), StandardCharsets.UTF_8).toCharArray();
        char[] keyPassword = new String(Base64.getDecoder().decode(reader.readLine()), StandardCharsets.UTF_8).toCharArray();
        try {
            KeyStore store;
            try { store = KeyStore.getInstance(new File(args[0]), storePassword); }
            catch (Exception failure) { System.out.println("STORE_PASSWORD_OR_FILE_INVALID"); return; }
            System.out.println("STORE_PASSWORD_VALID");
            if (!store.isKeyEntry(args[1])) { System.out.println("ALIAS_HAS_NO_PRIVATE_KEY"); return; }
            System.out.println("ALIAS_EXISTS");
            try {
                if (store.getKey(args[1], keyPassword) != null) {
                    System.out.println("CONFIGURED_KEY_PASSWORD_VALID");
                    return;
                }
            } catch (Exception failure) { /* Report only the outcome, never credentials or key bytes. */ }
            if (!Arrays.equals(storePassword, keyPassword)) {
                try {
                    if (store.getKey(args[1], storePassword) != null) {
                        System.out.println("KEY_USES_CURRENT_STORE_PASSWORD");
                        return;
                    }
                } catch (Exception failure) { /* Only the two currently configured passwords are checked. */ }
            }
            System.out.println("KEY_PASSWORD_INVALID");
        } finally {
            Arrays.fill(storePassword, '\0');
            Arrays.fill(keyPassword, '\0');
        }
    }
}
