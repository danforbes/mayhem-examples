cmake_minimum_required(VERSION 3.10)
project(FuzzExample NONE)  # No default language, we use custom commands

set(SRC "fuzz_target.cpp")
set(DOTNET_SRC "dotnet_fuzz_target.cs")
set(LIBFUZZER_SRC "libfuzzer_target.cpp")
set(OUTPUT_DIR "${CMAKE_SOURCE_DIR}/${OUT_DIR}")
set(OUTPUT_PACKAGE_DIR "${CMAKE_SOURCE_DIR}/${OUT_DIR}_packages")
file(MAKE_DIRECTORY ${OUTPUT_DIR})
file(MAKE_DIRECTORY ${OUTPUT_PACKAGE_DIR})

# Customize these as needed to refer to correct compiler paths
find_program(MSVC_CL cl.exe)
find_program(CLANG_CL clang++.exe)
find_program(CSC_CL csc.exe)
find_program(MINGW_CL g++.exe)

# Uncomment in cygwin. 
#set(ENV{PATH} "C:/cygwin64/bin;$ENV{PATH}")
#find_program(GCC_CL g++)

# Function: build fuzz binary
function(add_fuzz_target name src compiler_cmd)
    file(MAKE_DIRECTORY ${OUTPUT_DIR}/fuzz_target_${name})
    set(output "${OUTPUT_DIR}/fuzz_target_${name}/fuzz_target_${name}.exe")
    set(flags ${ARGN})

    if(${compiler_cmd} MATCHES "cl(.exe)?$")
        add_custom_command(
            OUTPUT ${output}
            COMMAND ${compiler_cmd} ${flags} ${CMAKE_SOURCE_DIR}/${src}
            DEPENDS ${SRC}
            COMMENT "Building fuzz_target_${name}.exe with MSVC"
        )
    else()
        add_custom_command(
            OUTPUT ${output}
            COMMAND ${compiler_cmd} ${flags} ${CMAKE_SOURCE_DIR}/${src} -o ${output}
            DEPENDS ${SRC}
            COMMENT "Building fuzz_target_${name}.exe with ${compiler_cmd}"
        )
    endif()

    add_custom_target(build_binary_${name} ALL DEPENDS ${output})
endfunction()

# Function: package binary for Mayhem
function(add_mayhem_package name)
    set(binary "${OUTPUT_DIR}/fuzz_target_${name}/fuzz_target_${name}.exe")
    set(package_dir "${OUTPUT_PACKAGE_DIR}/mayhem_package_${name}")
    set(package_marker "${package_dir}/.done")

    add_custom_command(
        OUTPUT ${package_marker}
        COMMAND ${CMAKE_COMMAND} -E make_directory ${package_dir}
        COMMAND mayhem package -o ${package_dir} ${binary}
        COMMAND ${CMAKE_COMMAND} -E copy_directory ${CMAKE_SOURCE_DIR}/testsuite ${package_dir}/testsuite
        COMMAND ${CMAKE_COMMAND} -E touch ${package_marker}
        DEPENDS ${binary}
        COMMENT "Packaging ${name} for Mayhem"
    )

    add_custom_target(package_${name} ALL DEPENDS ${package_marker})
    add_dependencies(package_${name} build_binary_${name})
endfunction()

# Targets

if(EXISTS "${MSVC_CL}")
    add_fuzz_target("msvc" "${SRC}" "${MSVC_CL}"
        /nologo /Zi /FS /MDd /EHsc
        /Fo${OUTPUT_DIR}/fuzz_target_msvc/fuzz_target_msvc.obj
        /Fe${OUTPUT_DIR}/fuzz_target_msvc/fuzz_target_msvc.exe
    )
    add_mayhem_package("msvc")

    add_fuzz_target("msvc_asan" "${SRC}" "${MSVC_CL}"
        /nologo /fsanitize=address /Zi /FS /EHsc /MDd
        /Fo${OUTPUT_DIR}/fuzz_target_msvc_asan/fuzz_target_msvc_asan.obj
        /Fe${OUTPUT_DIR}/fuzz_target_msvc_asan/fuzz_target_msvc_asan.exe
    )
    add_mayhem_package("msvc_asan")
endif()

if(EXISTS "${CLANG_CL}")
    add_fuzz_target("clang" "${SRC}" "${CLANG_CL}" -g -O1)
    add_mayhem_package("clang")

    add_fuzz_target("clang_asan" "${SRC}" "${CLANG_CL}" -g -O1 -fsanitize=address)
    add_mayhem_package("clang_asan")

    add_fuzz_target("libfuzzer" "${LIBFUZZER_SRC}" "${CLANG_CL}" -g -O1 -fsanitize=fuzzer)
    add_mayhem_package("libfuzzer")
endif()

if(EXISTS "${MINGW_CL}")
    add_fuzz_target("mingw" "${SRC}" "${MINGW_CL}" -g -O1)
    add_mayhem_package("mingw")
endif()

if(EXISTS "${GCC_CL}")
    add_fuzz_target("gplusplus" "${SRC}" "${GCC_CL}" -g -O1)
endif()



# Optional: .NET target (not packaged)
if(EXISTS "${CSC_CL}")
    file(MAKE_DIRECTORY "${OUTPUT_DIR}/fuzz_target_dotnet")
    set(DOTNET_OUTPUT "${OUTPUT_DIR}/fuzz_target_dotnet/fuzz_target_dotnet.exe")

    add_custom_command(
        OUTPUT ${DOTNET_OUTPUT}
        COMMAND ${CMAKE_COMMAND} -E make_directory ${OUTPUT_DIR}
        COMMAND ${CSC_CL} /unsafe /nologo /out:${DOTNET_OUTPUT} ${CMAKE_SOURCE_DIR}/${DOTNET_SRC}
        DEPENDS ${DOTNET_SRC}
        COMMENT "Building fuzz_target_dotnet.exe with ${CSC_CL}"
    )

    add_custom_target(build_dotnet ALL DEPENDS ${DOTNET_OUTPUT})
endif()