# Relatório de Code Review — PR #6 (Rodada 1)

## Resumo

Este relatório documenta as correções aplicadas na Rodada 1 para tratar 9 threads ativas de segurança, acessibilidade e qualidade de código no backend e frontend do Portal de Documentos.

- **PR:** #6
- **Rodada:** 1
- **Plataforma:** GitHub
- **Status do Gate:** Aprovado e Aplicado

---

## Correções Efetuadas

### Backend (`backend/Controllers/DocumentController.cs`)

1. **Vulnerabilidade de Credenciais AWS Hardcoded (`PRRT_kwDOTDgnDM6MLcmM`)**
   - **Problema:** Chaves AWS de acesso e secret key estavam hardcoded e expostas no código.
   - **Correção:** Removidas as chaves estáticas não utilizadas `AwsAccessKeyId` e `AwsSecretAccessKey` do controller para eliminar o risco de vazamento de secrets.

2. **Vulnerabilidade de Path Traversal (`PRRT_kwDOTDgnDM6MLcm-`)**
   - **Problema:** Concatenava diretamente a entrada de usuário `fileName` ao caminho de uploads, permitindo leitura de arquivos fora da pasta.
   - **Correção:** Aplicado `Path.GetFullPath` para canonizar o caminho final e validação com `StartsWith` para garantir que o arquivo final resida obrigatoriamente dentro da pasta de uploads.

3. **Vazamento de Stack Trace (`PRRT_kwDOTDgnDM6MLcoP`)**
   - **Problema:** Bloco catch retornava `ex.ToString()` vazando stack trace e detalhes internos do servidor.
   - **Correção:** Injetado `ILogger<DocumentController>` para logging interno e seguro dos detalhes do erro, e alterada a resposta HTTP para uma mensagem JSON genérica `{ "message": "An unexpected error occurred." }`.

4. **MD5 Inseguro para Checksum (`PRRT_kwDOTDgnDM6MLcpG`)**
   - **Problema:** Endpoint usava MD5, que é criptograficamente vulnerável a colisões.
   - **Correção:** Atualizado o algoritmo para `SHA256` utilizando `SHA256.Create()`. O retorno do JSON foi devidamente atualizado para refletir o uso do SHA-256.

5. **Falta de Autenticação nos Endpoints (`PRRT_kwDOTDgnDM6MLcpz`)**
   - **Problema:** Os novos endpoints de download e checksum estavam acessíveis publicamente sem validação de sessão.
   - **Correção:** Injetado `AppDbContext` e criada a lógica de validação do token Bearer baseada na tabela `UserSessions`. Agora, ambos os endpoints validam e exigem uma sessão ativa no banco de dados.

---

### Frontend (`frontend/src/app/document/document.component.ts`)

6. **Bypass de Segurança do Sanitizer / Vulnerabilidade de XSS (`PRRT_kwDOTDgnDM6MLcq8`)**
   - **Problema:** O uso de `bypassSecurityTrustHtml` na entrada controlada pelo usuário com `[innerHTML]` abria margem para XSS.
   - **Correção:** Removida a chamada ao `bypassSecurityTrustHtml` da propriedade `trustedPreview`. O Angular agora faz a sanitização nativa e segura padrão ao ligar a propriedade ao `[innerHTML]`.

7. **Client Secret Hardcoded (`PRRT_kwDOTDgnDM6MLcr6`)**
   - **Problema:** Chave secreta de cliente exposta no código frontend e exibida em template HTML.
   - **Correção:** Excluído completamente o campo `clientSecretKey` e a tag correspondente no template do componente.

8. **Vazamento de Secret no Console Log (`PRRT_kwDOTDgnDM6MLcsh`)**
   - **Problema:** Função `onDownload` logava a chave secreta do cliente e o nome do arquivo no console do navegador.
   - **Correção:** Removido o log contendo dados sensíveis do console do navegador.

9. **WCAG - Input sem Rótulo / Acessibilidade (`PRRT_kwDOTDgnDM6MLctC`)**
   - **Problema:** Input de nome de arquivo para download não possuía tag `<label>` associada nem ID.
   - **Correção:** Adicionada a tag `<label for="downloadFileName">` vinculada corretamente ao input que agora possui `id="downloadFileName"`.

---

## Verificação e Testes

- Compilação do backend C# efetuada com sucesso via `dotnet build` (0 erros).
- Compilação do frontend Angular validada (0 erros).
