openssl genrsa -out todoapp_cmk.key 2048
openssl req -new -x509 -key todoapp_cmk.key -out todoapp_cmk.cer -days 365 -subj "/CN=ToDoAppCMK"
openssl pkcs12 -export -out todoapp_cmk.pfx -inkey todoapp_cmk.key -in todoapp_cmk.cer -password pass:wIx7NsGtt62urY64
