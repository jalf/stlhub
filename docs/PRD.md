# PRD — Aplicativo de Catálogo de Objetos 3D - STLHub

## 1. Visão Geral

Criar um aplicativo para organizar, catalogar e recuperar arquivos de objetos 3D (`.stl`, `.3mf`, `.obj`). O sistema será focado em makers, designers, engenheiros e usuários de impressão 3D que possuem muitos arquivos dispersos em pastas e precisam de busca rápida e organização eficiente.

O app permitirá importar arquivos, enriquecer metadados e localizar modelos rapidamente via busca textual e tags.

O aplicativo se chama STLHub.

## 2. Problema

Usuários com bibliotecas grandes de modelos 3D enfrentam:

- Arquivos perdidos em pastas desorganizadas
- Nomes de arquivos ruins (`final_v2_ok.stl`)
- Dificuldade para achar modelos antigos
- Falta de preview visual rápido
- Falta de associação entre modelo e imagens/instruções
- Repetição de downloads/criações por não encontrar arquivos existentes

## 3. Objetivo do Produto

Centralizar e indexar bibliotecas pessoais/profissionais de objetos 3D, tornando simples:

- Armazenar
- Visualizar
- Buscar
- Classificar
- Reutilizar

## 4. Público-Alvo

- Makers / hobbyistas de impressão 3D
- Designers 3D
- Engenheiros mecânicos/produto
- Estúdios de prototipagem
- Pequenas empresas com acervo CAD/STL

## 5. MVP (Escopo Inicial)

### 5.1 Cadastro de Objetos 3D

Upload de arquivos:

- STL
- 3MF
- OBJ

Ao importar:

- Salvar arquivo original
- Extrair nome do arquivo
- Gerar thumbnail automático
- Ler dimensões quando possível
- Calcular hash para evitar duplicatas

### 5.2 Metadados Editáveis

Cada item terá:

- Nome
- Descrição
- Tags múltiplas
- Categoria (opcional)
- Data de importação
- Arquivos associados
- Thumbnail



### 5.2.1 Sistema de Categorias Hierárquicas

O sistema permitirá organizar objetos em categorias aninhadas, similar a uma estrutura de pastas.

Exemplos:

- Mecânica
  - Engrenagens
  - Rolamentos
- Casa
  - Cozinha
  - Banheiro
- Eletrônica
  - Cases
  - Suportes

Regras:

- Cada objeto pode pertencer a uma categoria principal
- Navegação por árvore expansível
- Mover categorias por drag and drop
- Renomear categorias
- Busca filtrando por categoria pai ou subcategoria
- Herança opcional de tags sugeridas por categoria

### 5.3 Thumbnail Automático

Sistema renderiza preview automaticamente:

- STL renderizado em malha cinza
- Fundo neutro
- Ângulo isométrico padrão

Fallback: obter o thumbnail a partir do sistems operacional ou ícone genérico se falhar.

### 5.4 Arquivos Associados

Permitir anexar ao objeto:

- Imagens (`png`, `jpg`)
- Gcode
- PDFs
- Instruções
- Arquivos ZIP
- Notas técnicas

### 5.5 Busca

#### Busca Full-text

Pesquisar por:

- Nome
- Descrição
- Nome de arquivos
- Tags

#### Busca por Filtros

- Tipo de arquivo
- Tags
- Data
- Categoria

### 5.6 Visualização

Lista/grid com:

- Thumbnail
- Nome
- Tags
- Tipo do arquivo
- Data

Tela de detalhe:

- Preview maior
- Metadados
- Arquivos anexos
- Histórico de edição (futuro)

## 6. Requisitos Não Funcionais

### Performance

- Busca em até 300ms com 10k arquivos
- Thumbnail gerado em background

### Armazenamento

- Suporte inicial local-first
- Biblioteca em disco local

### Confiabilidade

- Backup/exportação do catálogo
- Detecção de duplicatas por hash

### UX

- Drag and drop para importar
- Busca instantânea
- Interface simples

## 7. Fluxo Principal do Usuário

### Importar

1. Arrasta arquivos para app
2. Sistema processa
3. Gera thumbnails
4. Usuário adiciona tags/descrição

### Importar Pasta

1. Arrasta uma pasta para o app
2. Sistema varre recursivamente a hierarquia de diretórios
3. Cada subpasta é mapeada como categoria/subcategoria, reproduzindo a árvore de pastas original
4. Arquivos 3D encontrados (`.stl`, `.3mf`, `.obj`) são importados como objetos 3D na categoria correspondente
5. Demais arquivos na mesma pasta de um objeto 3D (imagens, PDFs, gcode, etc.) são adicionados como anexos desse objeto
6. Caso uma pasta contenha apenas arquivos não-3D (sem nenhum `.stl`, `.3mf` ou `.obj`), esses arquivos são ignorados
7. Categorias vazias (pastas sem objetos 3D em nenhum nível) não são criadas
8. Geração de thumbnails ocorre em background
9. Duplicatas são detectadas por hash e não reimportadas

#### Exemplo

Estrutura de pasta arrastada:

```
Mecânica/
  Engrenagens/
    engrenagem_608.stl
    engrenagem_608.pdf
    foto_montagem.jpg
  Rolamentos/
    rolamento_v2.3mf
    instrucoes.pdf
```

Resultado no app:

- **Categoria:** Mecânica → Engrenagens
  - **Objeto 3D:** `engrenagem_608.stl`
    - **Anexos:** `engrenagem_608.pdf`, `foto_montagem.jpg`
- **Categoria:** Mecânica → Rolamentos
  - **Objeto 3D:** `rolamento_v2.3mf`
    - **Anexos:** `instrucoes.pdf`

### Recuperar

1. Digita “engrenagem 608”
2. Filtra por STL
3. Abre item
4. Exporta ou abre pasta ou baixa o arquivo diretamente

## 8. Tecnologias Sugeridas

### Desktop App

- .NET + Avalonia UI

### Backend Local

- SQLite

### Busca

- SQLite FTS5

### Renderização Thumbnail

- Assimp + OpenGL / HelixToolkit / Blender headless

## 9. Modelo de Dados Simplificado

### Category

- Id
- Name
- ParentCategoryId (nullable)
- Path
- SortOrder


### Object3D

- Id
- Name
- Description
- MainFilePath
- FileType
- ThumbnailPath
- Hash
- CategoryId
- CreatedAt

### Tag

- Id
- Name

### ObjectTag

- ObjectId
- TagId

### Attachment

- Id
- ObjectId
- FilePath
- Type

## 10. Roadmap Futuro

### V2

- Visualizador 3D interativo embutido
- Auto-tagging com IA
- Sincronização cloud

### V3

- Marketplace pessoal
- Integração Thingiverse / Printables
- Versionamento de modelos

## 11. Métricas de Sucesso

- Tempo médio para achar arquivo < 10s
- % arquivos catalogados com tags
- Retenção semanal
- Quantidade de imports por usuário

## 12. Riscos

- Renderização inconsistente de STL corrompido
- Bibliotecas enormes (>100k arquivos)
- Duplicatas com nomes diferentes
- UX ruim ao editar muitos itens

## 13. Recomendação Direta

Produto ideal como **desktop local-first em C# + Avalonia + SQLite FTS5**.
