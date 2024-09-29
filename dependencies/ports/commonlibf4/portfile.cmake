vcpkg_from_github(
  OUT_SOURCE_PATH SOURCE_PATH
  REPO shad0wshayd3-FO4/CommonLibF4
  REF 739a2ed87d28f4aac1d50527fae104a5eed91f69
  SHA512 1e49e6a7a5b58181a12882f86428af68f630ad4e4349cbcc2af105082fa22f9459711ef883d4269097a3998e9bac7c2bec8fde79217d924969a399a4fe33c18d
  HEAD_REF master
)

# set(SOURCE_PATH "C:\\Users\\Nikita\\Workspace\\skyrim-mod-workspace\\CommonLibF4-merge-ws\\CommonLibF4")
if(VCPKG_HOST_IS_WINDOWS AND VCPKG_TARGET_IS_WINDOWS AND NOT VCPKG_TARGET_IS_MINGW AND DEFINED $ENV{VisualStudioVersion})
  set(MSVC_RUNTIME_VCPKG_CONFIG_ARG "-DCMAKE_MSVC_RUNTIME_LIBRARY=\"MultiThreaded$<$<CONFIG:Debug>:Debug>DLL\"")

  # 2022 requires `/external:anglebrackets` to build with the above argument
  if($ENV{VisualStudioVersion} STREQUAL "17.0")
    set(CXX_FLAGS_VCPKG_CONFIG_ARG "-DCMAKE_CXX_FLAGS=\"/EHsc /MP /W4 /WX /external:anglebrackets /external:W0\"")
  else()
    set(CXX_FLAGS_VCPKG_CONFIG_ARG "-DCMAKE_CXX_FLAGS=\"/EHsc /MP /W4 /WX /external:W0\"")
  endif()
else()
  set(MSVC_RUNTIME_VCPKG_CONFIG_ARG "")
  set(CXX_FLAGS_VCPKG_CONFIG_ARG "")
endif()

# set(MSVC_RUNTIME_ARG "Some Text")
vcpkg_configure_cmake(
  SOURCE_PATH "${SOURCE_PATH}"
  PREFER_NINJA
  OPTIONS
  -DF4SE_SUPPORT_XBYAK=ON
  -DCMAKE_EXE_LINKER_FLAGS_RELEASE="/DEBUG:FASTLINK"
  ${MSVC_RUNTIME_VCPKG_CONFIG_ARG}
  ${CXX_FLAGS_VCPKG_CONFIG_ARG}
)

vcpkg_install_cmake()
vcpkg_copy_pdbs()
vcpkg_cmake_config_fixup(
  PACKAGE_NAME CommonLibF4
  CONFIG_PATH "lib/cmake/CommonLibF4"
)
file(GLOB CMAKE_CONFIGS "${CURRENT_PACKAGES_DIR}/share/CommonLibF4/CommonLibF4/*.cmake")
file(INSTALL ${CMAKE_CONFIGS} DESTINATION "${CURRENT_PACKAGES_DIR}/share/CommonLibF4")

file(REMOVE_RECURSE "${CURRENT_PACKAGES_DIR}/debug/include")
file(REMOVE_RECURSE "${CURRENT_PACKAGES_DIR}/share/CommonLibF4/CommonLibF4")

# file(MAKE_DIRECTORY "${CURRENT_PACKAGES_DIR}/include/CommonLibF4")
# file(RENAME "${CURRENT_PACKAGES_DIR}/include/F4SE" "${CURRENT_PACKAGES_DIR}/include/CommonLibF4/F4SE")
# file(RENAME "${CURRENT_PACKAGES_DIR}/include/RE" "${CURRENT_PACKAGES_DIR}/include/CommonLibF4/RE")
# file(RENAME "${CURRENT_PACKAGES_DIR}/include/REL" "${CURRENT_PACKAGES_DIR}/include/CommonLibF4/REL")
file(
  INSTALL "${SOURCE_PATH}/LICENSE"
  DESTINATION "${CURRENT_PACKAGES_DIR}/share/${PORT}"
  RENAME copyright)
