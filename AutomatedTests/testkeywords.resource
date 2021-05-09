*** Keywords ***

Check File Menu
    # this assumes brainsimulator is started...
    Wait And Click		bs2_file_menu
    Wait For Center     bs2_file_new_item
    Wait For Center     bs2_file_open_item
    Wait For Center     bs2_file_save_item_disabled
    Wait For Center     bs2_file_saveas_item
    Wait For Center     bs2_file_properties_item
    Wait For Center     bs2_file_recent_item
    Wait For Center     bs2_file_library_item
    Wait For Center     bs2_file_exit_item
    