import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ModalNameDialogComponent } from './modal-name-dialog.component';

describe('ModalNameDialogComponent', () => {
  let component: ModalNameDialogComponent;
  let fixture: ComponentFixture<ModalNameDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ModalNameDialogComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ModalNameDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
