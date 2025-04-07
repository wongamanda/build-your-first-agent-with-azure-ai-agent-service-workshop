## Introduction

The Code Interpreter includes a limited set of Latin-based fonts for visualizations. In this lab, we’ll add support for Arabic, Chinese, Hindi, Korean, and Japanese. While you can add other languages, these are the ones selected for the workshop.

Just a reminder: the Code Interpreter runs in a sandboxed Python environment, which means it can’t download fonts from the internet. To work around this, we’ll upload a ZIP file containing the necessary fonts. The Code Interpreter will then extract the fonts to a predefined location and use them to render visualizations.

## Lab Exercise

Multilingual support isn’t enabled in the earlier labs because it requires uploading a large ZIP file with the necessary fonts and attaching it to the Code Interpreter—something that takes too much time for each lab. Instead, we enable this support in this lab.

## Rerun the previous lab

First, we're going to rerun the previous lab so we can see how the Code Interpreter supports multilingual text.

1. Start the agent app by pressing <kbd>F5</kbd>.
2. In the terminal, the app will start, and the agent app will prompt you to  **Enter your query**.
3. Try these questions:

      1. `What were the sales by region for 2022`
      2. `In Korean`
      3. `Show as a pie chart`

4. Review the visualization and you'll see that the text is not rendered correctly. This is because the Code Interpreter doesn't have the necessary fonts to render non-Latin characters.
5. When you're done, type **exit** to clean up the agent resources and stop the app.

## Add Multilingual Support

=== "Python"

    1. Open the `main.py`.

    2. Define a new instructions file for our agent: **uncomment** the following lines by removing the **"# "** characters

        ```python
        INSTRUCTIONS_FILE = "instructions/code_interpreter_multilingual.txt"

        font_file_info = await utilities.upload_file(project_client, utilities.shared_files_path / FONTS_ZIP)
        code_interpreter.add_file(file_id=font_file_info.id)
        ```

        !!! warning
            The lines to be uncommented are not adjacent. When removing the # character, ensure you also delete the space that follows it.

    3. Review the code in the `main.py` file.

        After uncommenting, your code should look like this:

        ```python
        INSTRUCTIONS_FILE = "instructions/function_calling.txt"
        INSTRUCTIONS_FILE = "instructions/code_interpreter.txt"
        INSTRUCTIONS_FILE = "instructions/file_search.txt"
        INSTRUCTIONS_FILE = "instructions/code_interpreter_multilingual.txt"
        # INSTRUCTIONS_FILE = "instructions/bing_grounding.txt"


        async def add_agent_tools() -> None:
            """Add tools for the agent."""
            font_file_info = None

            # Add the functions tool
            toolset.add(functions)

            # Add the code interpreter tool
            code_interpreter = CodeInterpreterTool()
            toolset.add(code_interpreter)

            # Add the tents data sheet to a new vector data store
            vector_store = await utilities.create_vector_store(
                project_client,
                files=[TENTS_DATA_SHEET_FILE],
                vector_store_name="Contoso Product Information Vector Store",
            )
            file_search_tool = FileSearchTool(vector_store_ids=[vector_store.id])
            toolset.add(file_search_tool)

            # Add multilingual support to the code interpreter
            font_file_info = await utilities.upload_file(project_client, utilities.shared_files_path / FONTS_ZIP)
            code_interpreter.add_file(file_id=font_file_info.id)

            # Add the Bing grounding tool
            # bing_connection = await project_client.connections.get(connection_name=BING_CONNECTION_NAME)
            # bing_grounding = BingGroundingTool(connection_id=bing_connection.id)
            # toolset.add(bing_grounding)

            return font_file_info
        ```

=== "C#"

    1. Open the `Program.cs` file.

    2. **Update** the creation of the lab to use the `Lab2` class.

        ``` csharp
        await using Lab lab = new Lab4(projectClient, apiDeploymentName);
        ```

    3. Review the `Lab4.cs` class to see how the Code Interpreter is added to the Tools list.

## Review the Instructions

1. Open the **src/workshop/instructions/code_interpreter_multilingual.txt** file. This file replaces the instructions used in the previous lab.
2. The **Tools** section now includes an extended “Visualization and Code Interpretation” section describing how to create visualizations and handle non-Latin languages.

The following is a summary of the instructions given to the Code Interpreter:

- **Font Setup for Non-Latin Scripts (e.g., Chinese, Korean, Hindi):**
  - On first run, verify if the `/mnt/data/fonts` folder exists. If missing, unzip the font file into this folder.
  - **Available Fonts:**
    - Arabic: `CairoRegular.ttf`
    - Chinese: `NotoSansSCRegular.ttf`
    - Hindi: `NotoSansDevanagariRegular.ttf`
    - Korean: `NanumGothicRegular.ttf`
    - Japanese: `NotoSansJPRegular.ttf`

- **Font Usage:**
  - Load the font with `matplotlib.font_manager.FontProperties` using the correct path.
  - Apply the font to:
    - `plt.title()` using the `fontproperties` parameter.
    - All labels and text using `textprops={'fontproperties': font_prop}` in functions like `plt.pie()` or `plt.bar_label()`.
  - Ensure all text (labels, titles, legends) is properly encoded, without boxes or question marks.

- **Visualization Text:**
  - Always translate the data to the requested or inferred language (e.g., Chinese, French, English).
  - Use the appropriate font from `/mnt/data/fonts/fonts` for all chart text (e.g., titles, labels).

## Run the Agent App

1. Press <kbd>F5</kbd> and select whether you want to run the C# or Python app.
2. In the terminal, the app will start, and the agent app will prompt you to  **Enter your query**.

### Start a Conversation with the Agent

Try these questions:

1. `What were the sales by region for 2022`
2. `In Korean`
3. `Show as a pie chart`
4. `Show in Chinese`

## Stop the Agent App

When you're done, type **exit** to clean up the agent resources and stop the app.