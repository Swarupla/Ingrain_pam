import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { FilesMappingModalComponent } from './files-mapping-modal.component';

describe('FilesMappingModalComponent', () => {
  let component: FilesMappingModalComponent;
  let fixture: ComponentFixture<FilesMappingModalComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ FilesMappingModalComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(FilesMappingModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
